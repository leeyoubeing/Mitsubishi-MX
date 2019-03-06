# Mitsubishi-MX
利用三菱的MX Component与三菱PLC通讯

MXObject.cs		实现MXObject对象  
Win32API.cs		需要使用的相关Win32 API的声明

# 一、使用方法  
	1. 创建MXObject对象  
		plc = new MXObject(form, logicalno, password);
	
	2. 使用MXObject对象的AddTag方法增加PLC变量到TagList4Random，或者使用AddBlock方法增加要读的块到TagList4Block
		plc.AddTag("D20", Tag_DatType.STRING, 10);
	or
		List<PLC_Tag> taglist = new List<PLC_Tag>();
		PLC_Tag tag;
		tag = new PLC_Tag();
		tag.Address = "D100";
		tag.DataType = Tag_DatType.INT16;
		taglist.Add(tag);
		......
		plc.AddBlock("D100", 30, taglist);
		
	3. 绑定事件
		plc.OnConnectPLC += ...;
		plc.OnUpdateTagValue += ...;
		plc.OnReadRandomComplete += ...;
		plc.OnReadBlockComplete += ...;
		plc.OnWriteRandomComplete += ...;
		plc.OnWriteBlockComplete += ...;
	
	4. 调用MXObject的Start方法开始通讯
	
	5. 调用MXObject的Stop方法停止通讯
	
# 二、API
	1. class PLC_Tag  
		a. 重要属性：  
			Parent: 对应的MXObject  
			Address: 地址  
			Tag_DataType: 数据类型  
			DataLength: 当数据类型为String时，字符串长度，其他数据类型不用设置长度   
			Quality: 数据正确与否  
			Value: 数据类型不为string时Tag的值  
			szValue: 数据类型为string时Tag的值  
			associatedCtrls: 与此Tag相关联的控件。Tag的值发生变化时方便更新控件  
		b. 构造函数
			PLC_Tag(MXObject parent, string address, Tag_DataType data_type, int length = -1, Control[] controls = null)
			当数据类型为string是，必须指定长度 length
			
		c. Read()
			读一次数据
			当parent==null时，返回false
			
		d. Write(object Value)
			写一次数据。Value的实际数据类型必须与此Tag的数据类型相符
			当parent==null时，返回false
			
		e. ToString()
			以字符串的形式返回此Tag的值。当Quality为QULITY_BAD时，返回“Err”
			
			
	2. class MXObject
		a. 重要属性：
			LogicalNo: logical number
			Password: password
			Comment: comment
			上面三项是在Communication Setup Utility中设置的值
			Running: 通讯线程是否在运行
			Connected: 与PLC的连接状态
			TagList4Random: 随机读（循环）时的Tag列表
			TagList4Block: 读数据块（循环）是的数据块列表
			
		b. 构造函数
			MXObject(Form Owner, int logicalNo, string password)
			
		c. PLC_Tag AddTag(string address, Tag_DataType data_type, int length = -1)
			增加一个Tag到TagList4Random。当数据类型为string时，必须设定length
			当Connected为True时，返回null
			
		d. bool AddBlock(string start_addr, int count, List<PLC_Tag> tags = null)
			增加一个BlockTagInfo到TagList4Block。调用此函数前，要先产生一个taglist
			当Connected为True时，返回false
			
		e. bool Start()
			开始通讯
			
		f. void Stop()
			停止通讯
			
		g. bool ReadRandom(string address_list)
			异步随机读一次数据。读完成将发生OnReadRandomComplete事件
			address_list为以逗号分隔的地址列表
		
		h. bool ReadBlock(string start_address, int count)
			异步读数据块一次。读完成将发生OnReadBlockComplete事件
		
		i. bool WriteRandom(string address_list, object[] Values)
			异步随机写数据。写完成发生OnWriteRandomComplete事件
			address_list为以逗号分隔的地址列表
			Values中的数据必须有明确的数据类型
			注意地址数量与数据个数要相等
			
		j. bool WriteBlock(string start_address, short[] Values)
			异步写数据块。写完成发生OnWriteBlockComplete事件
			
		k. static PLCSimpleInfo[] GetPLCInfo()
			读取Communication Setup Utility的设置
			
		l. static void EnumPLCTags(Form form, Control control, List<MXObject> plcs)
			当窗体中控件的tag属性设置了plc tag 信息时，调用此方法可以自动生成MXObject列表，并生成PLC_Tag，增加到TagList4Random中
		
		m. 连接事件
			public delegate void ConnectEventHandler(MXObject Sender, int conn_value, int info);
			public event ConnectEventHandler OnConnectPLC
			连接PLC时发生此事件
			conn_value == 0，连接成功
			conn_value == -1，读写过程中连接中断
			conn_value == -2，连接正常断开
			conn_value == 其他，连接失败（参考MX Component 手册）
			info 暂时没用
			
		n. Tag值更新事件
			public delegate void UpdateTagValueHandler(MXObject Sender, int TagListType);
			public event UpdateTagValueHandler OnUpdateTagValue
			TagList4Random或者TagList4Block读取完成时发生此事件
			TagListType == 0, TagList4Random读取完成
			TagListType == 1, TagList4Block读取完成
			
		o. 异步随机读完成事件
			public delegate void ReadRandomCompleteHandler(MXObject Sender, string AddressList, short[] Values);
			public event ReadRandomCompleteHandler OnReadRandomComplete
			Values == null，读失败
			
		p. 异步读数据块完成事件
			public delegate void ReadBlockCompleteHandler(MXObject Sender, string StartAddress, short[] Values);
			public event ReadBlockCompleteHandler OnReadBlockComplete
			Values == null，读失败
			
		q. 异步随机写完成事件
			public delegate void WriteRandomCompleteHandler(MXObject Sender, string AddressList, bool Succeeded);
			public event WriteRandomCompleteHandler OnWriteRandomComplete
			Succeeded表示写操作是否成功
			
		r. 异步写数据块完成事件
			public delegate void WriteBlockCompleteHandler(MXObject Sender, string StartAddress, bool Succeeded);
			public event WriteBlockCompleteHandler OnWriteBlockComplete
			Succeeded表示写操作是否成功
			
