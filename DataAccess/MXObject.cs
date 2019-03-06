//Programmed by Binbinsoft
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using WindowsAPI;

namespace PLCDataAccess
{
    public struct PLCSimpleInfo
    {
        public int LogicalNo;
        public string Password;
        public string Comment;
    }
    public enum Tag_DataType
    {
        UNKNOWN, BIT, INT16, UINT16, INT32, UINT32, FLOAT, STRING,
    }
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Tag_Value
    {
        [FieldOffset(0)]
        public bool bValue;
        [FieldOffset(0)]
        public Int16 i2Value;
        [FieldOffset(0)]
        public UInt16 u2Value;
        [FieldOffset(0)]
        public Int32 i4Value;
        [FieldOffset(0)]
        public UInt32 u4Value;
        [FieldOffset(0)]
        public float fValue;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockTagInfo
    {
        public string Address;
        public int Count;
        public List<PLC_Tag> Tags;
    }
    #region struct WRDataBlock
    //读写时用到的数据块
    [StructLayout(LayoutKind.Sequential)]
    public struct WRDataBlock
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MXObject.BUFFER_SIZE, ArraySubType = UnmanagedType.I2)]
        public short[] data;//数据
        [MarshalAs(UnmanagedType.LPTStr)]
        public string address;//读写地址，多地址之间用\n分隔
        [MarshalAs(UnmanagedType.LPTStr)]
        public string original_address;//原始地址
        public short dat_count;//长度, <=BUFFER_SIZE
    }
    #endregion struct WRDataBlock

    public class PLC_Tag
    {
        public const byte QUALITY_GOOD = 0;
        public const byte QUALITY_BAD = 1;
        private string _tag_address;
        public string Address
        {
            get
            {
                return _tag_address;
            }
            set
            {
                if (value != _tag_address)
                {
                    _tag_address = value;
                    SplitAddress(_tag_address, out StrPart, out NumPart, out AddressBase);
                }
            }
        }
        //地址是用10进制还是16进制
        public byte AddressBase = 10;
        public MXObject Parent { get; set; }
        public Tag_DataType DataType { get; set; }
        //数据类型为字符串时，字符串长度（不包括\0)
        public int DataLength { get; set; }
        //地址的字符部分
        public string StrPart = "";
        //地址的数值部分
        public int NumPart = -1;
        public Tag_Value Value;
        //当数据类型位String是，存储字符串值
        public string szValue = "";
        public byte Quality = QUALITY_BAD;
        //与此Tag关联的控件，以方便更新控件的显示
        public List<Control> associatedCtrls = new List<Control>();

        public PLC_Tag()
        {
            DataLength = -1;
            Parent = null;
        }
        public PLC_Tag(MXObject parent, string address, Tag_DataType data_type, int length = -1, Control[] controls = null)
        {
            Parent = parent;
            this.Address = address;
            this.DataType = data_type;
            DataLength = length;
            if (null != controls)
            {
                foreach (Control ctrl in controls)
                    associatedCtrls.Add(ctrl);
            }
        }
        public bool Read()
        {
            if (null != Parent)
            {
                return Parent.ReadRandom(Address);
            }
            else
                return false;
        }
        public bool Write(object value)
        {
            if (null != Parent)
            {
                object[] v = null;
                if ((DataType == Tag_DataType.STRING && value.GetType() == typeof(string))
                    || (DataType == Tag_DataType.BIT && value.GetType() == typeof(bool)))
                {
                    v = new object[] { value };
                }
                else
                {
                    switch (DataType)
                    {
                        case Tag_DataType.INT16:
                            v = new object[] { (short)value };
                            break;
                        case Tag_DataType.UINT16:
                            v = new object[] { (ushort)value };
                            break;
                        case Tag_DataType.INT32:
                            v = new object[] { (int)value };
                            break;
                        case Tag_DataType.UINT32:
                            v = new object[] { (uint)value };
                            break;
                        case Tag_DataType.FLOAT:
                            v = new object[] { (float)value };
                            break;
                    }
                }
                if (null == v) return false;
                return Parent.WriteRandom(Address, v);
            }
            else
                return false;
        }
        public override String ToString()
        {
            if (Quality == QUALITY_BAD)
                return "Err";

            switch (DataType)
            {
                case Tag_DataType.BIT:
                    return Value.bValue.ToString();
                case Tag_DataType.INT16:
                    return Value.i2Value.ToString();
                case Tag_DataType.UINT16:
                    return Value.u2Value.ToString();
                case Tag_DataType.INT32:
                    return Value.i4Value.ToString();
                case Tag_DataType.UINT32:
                    return Value.u4Value.ToString();
                case Tag_DataType.FLOAT:
                    return Value.fValue.ToString();
                case Tag_DataType.STRING:
                    return szValue;
                default: return "data type error";
            }
        }
        //把地址分成两部分：字符，数值
        public static void SplitAddress(string address, out string strPLCTagType, out int nAddress, out byte addr_base)
        {
            string s = address.ToUpper();
            string s1 = s.Substring(0, 1);
            string s2, s3;
            switch (s1)
            {
                case "A":
                case "B":
                case "D":
                case "F":
                case "L":
                case "M":
                case "R":
                case "V":
                case "W":
                case "X":
                case "Y":
                    s2 = s.Substring(1);
                    break;
                case "Z":
                    s3 = s.Substring(0, 2);
                    if (s3.Equals("ZR"))
                    {
                        s1 = s3;
                        s2 = s.Substring(2);
                    }
                    else
                        s2 = s.Substring(1);
                    break;
                case "C":
                    s3 = s.Substring(0, 2);
                    switch (s3)
                    {
                        case "CC":
                        case "CM":
                        case "CN":
                        case "CS":
                        case "CT":
                            s1 = s3;
                            s2 = s.Substring(2);
                            break;
                        default:
                            s2 = s.Substring(1);
                            break;
                    }
                    break;
                case "S":
                    s3 = s.Substring(0, 2);
                    switch (s3)
                    {
                        case "SB":
                        case "SC":
                        case "SD":
                        case "SM":
                        case "SN":
                        case "SS":
                        case "SW":
                            s1 = s3;
                            s2 = s.Substring(2);
                            break;
                        default:
                            s2 = s.Substring(1);
                            break;
                    }
                    break;
                case "T":
                    s3 = s.Substring(0, 2);
                    switch(s3)
                    {
                        case "TC":
                        case "TM":
                        case "TN":
                        case "TS":
                        case "TT":
                            s1 = s3;
                            s2 = s.Substring(2);
                            break;
                        default:
                            s2 = s.Substring(1);
                            break;
                    }
                    break;
                default:
                    throw new Exception("Invalid Address.");
            }
            strPLCTagType = s1;
            if (s1.Equals("X") || s1.Equals("Y") || s1.Equals("B") || s1.Equals("W"))
            {
                nAddress = Convert.ToInt32(s2, 16);
                addr_base = 16;
            }
            else
            {
                nAddress = Convert.ToInt32(s2);
                addr_base = 10;
            }
        }
    }

    public class MXObject: Control
    {
        //conn_value=-1: 连接中断
        public delegate void ConnectEventHandler(MXObject Sender, int conn_value, int info);
        private ConnectEventHandler _OnTMConnect;
        public event ConnectEventHandler OnConnectPLC
        {
            add
            {
                _OnTMConnect += value;//new ConnectEventHandler(value);
            }
            remove
            {
                _OnTMConnect -= value;// new ConnectEventHandler(value);
            }
        }
        public delegate void UpdateTagValueHandler(MXObject Sender, int TagListType);
        private UpdateTagValueHandler _OnUpdateTagValue;
        public event UpdateTagValueHandler OnUpdateTagValue
        {
            add
            {
                _OnUpdateTagValue += value;
            }
            remove
            {
                _OnUpdateTagValue -= value;
            }
        }

        public delegate void ReadRandomCompleteHandler(MXObject Sender, string AddressList, short[] Values);
        private ReadRandomCompleteHandler _OnReadRandomComplete;
        public event ReadRandomCompleteHandler OnReadRandomComplete
        {
            add
            {
                _OnReadRandomComplete += value;
            }
            remove
            {
                _OnReadRandomComplete -= value;
            }
        }

        public delegate void ReadBlockCompleteHandler(MXObject Sender, string StartAddress, short[] Values);
        private ReadBlockCompleteHandler _OnReadBlockComplete;
        public event ReadBlockCompleteHandler OnReadBlockComplete
        {
            add
            {
                _OnReadBlockComplete += value;
            }
            remove
            {
                _OnReadBlockComplete -= value;
            }
        }

        public delegate void WriteRandomCompleteHandler(MXObject Sender, string AddressList, bool Succeeded);
        private WriteRandomCompleteHandler _OnWriteRandomComplete;
        public event WriteRandomCompleteHandler OnWriteRandomComplete
        {
            add
            {
                _OnWriteRandomComplete += value;
            }
            remove
            {
                _OnWriteRandomComplete -= value;
            }
        }

        public delegate void WriteBlockCompleteHandler(MXObject Sender, string StartAddress, bool Succeeded);
        private WriteBlockCompleteHandler _OnWriteBlockComplete;
        public event WriteBlockCompleteHandler OnWriteBlockComplete
        {
            add
            {
                _OnWriteBlockComplete += value;
            }
            remove
            {
                _OnWriteBlockComplete -= value;
            }
        }

        public const int BUFFER_SIZE = 1024;

        public static string[] ValueTypeStr = { "B", "I2", "U2", "I4", "U4", "F", "S" };
        public static string[] PLCTagTypeStr = {
            "X", "Y", "B", "W", "SB", "DX", "DY", "M", "SM", "L", "F", "V",
            "D", "SD", "R", "ZR", "S", "SW", "TC", "TS", "TN", "CC",
            "CS", "CN", "SC", "SS", "SN", "Z", "TT", "TM", "CT", "CM", "A"
        };

        public int LogicalNo { get; set; }
        public string Password { get; set; }
        public string Comment { get; set; }

        //last error message
        public string LastErrorMsg = "";
        //是否已经成功连接
        public bool Connected = false;
        private int __connect_value = -1;
        //线程是否正在运行
        public bool Running = false;
        //停止线程的事件对象
        public AutoResetEvent StopEvent = new AutoResetEvent(false);

        //以随机读的方式获取值的tag列表
        public List<PLC_Tag> TagList4Random = new List<PLC_Tag>();
        //以\n分隔的地址列表(随机读的Tag）
        public string AddrList4Random = "";
        //TagList4Random中所有Tag的字数(wordcount)
        public int WordCounts4Random = 0;

        //以成块读的方式获取值的tag列表
        public List<BlockTagInfo> TagList4Block = new List<BlockTagInfo>();

        private WorkThread _thread = null;

        #region "Constructor"
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Owner">此对象的父对象</param>
        /// <param name="logicalNo">logical number</param>
        /// <param name="password"> password </param>
        public MXObject(Form Owner, int logicalNo, string password)
        {
            this.Parent = Owner;
            Visible = false;
            LogicalNo = logicalNo;
            Password = password;
        }
        #endregion "Constructor"

        #region "destructor"
        ~MXObject()
        {
            Stop();
        }
        #endregion "destructor"
        #region "InitialAddressString"
        private void InitialAddressString()
        {
            WordCounts4Random = 0;
            AddrList4Random = "";
            foreach (PLC_Tag _tag in TagList4Random)
            {
                AddrList4Random += _tag.Address + "\n";
                WordCounts4Random++;
                if (_tag.DataType == Tag_DataType.INT32 || _tag.DataType == Tag_DataType.UINT32 || _tag.DataType == Tag_DataType.FLOAT)
                {
                    AddrList4Random += _tag.StrPart + Convert.ToString(_tag.NumPart + 1, _tag.AddressBase) + "\n";
                    WordCounts4Random++;
                }
                else
                if (_tag.DataType == Tag_DataType.STRING)
                {
                    int k = (_tag.DataLength + 1) / 2;
                    for (int i = 1; i < k; i++)
                    {
                        AddrList4Random += _tag.StrPart + Convert.ToString(_tag.NumPart + i, _tag.AddressBase) + "\n";
                        WordCounts4Random++;
                    }
                }
            }
        }
        #endregion "InitialAddressString"
        #region "DefWndProc"
        protected override void DefWndProc(ref Message m)
        {
            int wparam, lparam;
            WRDataBlock wdb;
            LastErrorMsg = "";
            switch (m.Msg)
            {
                case WorkThread.TM_THREAD_FINISHED:
                    _thread.ThreadId = UInt32.MaxValue;
                    _thread.threadObj = null;
                    _thread = null;
                    break;
                case WorkThread.TM_CONNECTION:
                    wparam = (int)m.WParam;
                    if (__connect_value != wparam)
                    {
                        __connect_value = wparam;
                        Connected = wparam == 0;
                        if (_OnTMConnect != null)
                        {
                            _OnTMConnect(this, (int)m.WParam, (int)m.LParam);
                        }
                        //_OnTMConnect?.Invoke(this, (int)m.WParam, (int)m.LParam);
                    }
                    break;
                case WorkThread.TM_NORMAL_READ:
                    if (_OnUpdateTagValue != null)
                    {
                        _OnUpdateTagValue(this, (int)m.WParam);
                    }
                    //_OnUpdateTagValue?.Invoke(this, (int)m.WParam);
                    break;
                case WorkThread.TM_READ_RANDOM:
                    try
                    {
                        //wdb = Marshal.PtrToStructure<WRDataBlock>(m.WParam);//vs2017
                        wdb = (WRDataBlock)Marshal.PtrToStructure(m.WParam, typeof(WRDataBlock));
                        Marshal.FreeHGlobal(m.WParam);

                        lparam = (int)m.LParam;
                        short[] dat = null;
                        if (lparam >= 0)//read succeeded
                        {
                            dat = new short[wdb.dat_count];
                            Array.Copy(wdb.data, dat, wdb.dat_count);
                        }
                        if (_OnReadRandomComplete != null)
                        {
                            _OnReadRandomComplete(this, wdb.original_address, dat);
                        }
                        //_OnReadRandomComplete?.Invoke(this, wdb.original_address, dat);
                    }
                    catch (Exception e)
                    {
                        LastErrorMsg = e.Message;
                    }
                    break;
                case WorkThread.TM_WRITE_RANDOM:
                    try
                    {
                        //wdb = Marshal.PtrToStructure<WRDataBlock>(m.WParam);//vs2017
                        wdb = (WRDataBlock)Marshal.PtrToStructure(m.WParam, typeof(WRDataBlock));
                        Marshal.FreeHGlobal(m.WParam);

                        lparam = (int)m.LParam;
                        if (_OnWriteRandomComplete != null)
                        {
                            _OnWriteRandomComplete(this, wdb.original_address, 0 == lparam);
                        }
                        //_OnWriteRandomComplete?.Invoke(this, wdb.original_address, 0 == lparam);
                    }
                    catch (Exception e)
                    {
                        LastErrorMsg = e.Message;
                    }
                    break;
                case WorkThread.TM_READ_BLOCK:
                    try
                    {
                        //wdb = Marshal.PtrToStructure<WRDataBlock>(m.WParam);//vs2017
                        wdb = (WRDataBlock)Marshal.PtrToStructure(m.WParam, typeof(WRDataBlock));
                        Marshal.FreeHGlobal(m.WParam);

                        lparam = (int)m.LParam;
                        short[] dat = null;
                        if (lparam >= 0)//read succeeded
                        {
                            dat = new short[wdb.dat_count];
                            Array.Copy(wdb.data, dat, wdb.dat_count);
                        }
                        if (_OnReadBlockComplete != null)
                        {
                            _OnReadBlockComplete(this, wdb.original_address, dat);
                        }
                        //_OnReadBlockComplete?.Invoke(this, wdb.original_address, dat);
                    }
                    catch (Exception e)
                    {
                        LastErrorMsg = e.Message;
                    }
                    break;
                case WorkThread.TM_WRITE_BLOCK:
                    try
                    {
                        //wdb = Marshal.PtrToStructure<WRDataBlock>(m.WParam);//vs2017
                        wdb = (WRDataBlock)Marshal.PtrToStructure(m.WParam, typeof(WRDataBlock));
                        Marshal.FreeHGlobal(m.WParam);

                        lparam = (int)m.LParam;
                        if (_OnWriteBlockComplete != null)
                        {
                            _OnWriteBlockComplete(this, wdb.original_address, 0 == lparam);
                        }
                        //_OnWriteBlockComplete?.Invoke(this, wdb.original_address, 0 == lparam);
                    }
                    catch (Exception e)
                    {
                        LastErrorMsg = e.Message;
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion "DefWndProc"

        #region "Start"
        public bool Start()
        {
            if (Connected) return false;
            InitialAddressString();
            _thread = new WorkThread(this.Handle);
            _thread.Start(this);
            Running = true;
            return true;
        }
        #endregion "Start"

        #region "Stop"
        public void Stop()
        {
            StopEvent.Set();
            _thread.Stop();
            Running = false;
//            Connected = false;
        }
        #endregion "Stop"
        //把一个TAG增加到随机读的列表 (TagList4Random)
        public PLC_Tag AddTag(string address, Tag_DataType data_type, int length = -1)
        {
            if (Connected) return null;
            PLC_Tag atag = new PLC_Tag(this, address, data_type, length);
            TagList4Random.Add(atag);
            return atag;
        }
        //增加成块读到TagList4Block
        public bool AddBlock(string start_addr, int count, List<PLC_Tag> tags = null)
        {
            if (Connected) return false;
            BlockTagInfo bti = new BlockTagInfo
            {
                Address = start_addr,
                Count = count,
                Tags = tags
            };
            TagList4Block.Add(bti);
            return true;
        }
        #region "ReadRandom"
        /// <summary>
        /// 异步单次随机读数据
        /// </summary>
        /// <param name="address_list">PLC地址列表</param>
        /// <returns>PLC未连接，或数据超过 BUFFER_SIZE 返回 FALSE</returns>
        public bool ReadRandom(string address_list)
        {
            if (!Connected) return false;
            string s = address_list.TrimEnd(',');
            if (s.Length == 0) return false;
            string ss = "";
            short n = 1;
            foreach (char c in s)
            {
                if (c == ',')
                {
                    ss += '\n';
                    n++;
                }
                else
                    ss += c;
            }
            if (n > BUFFER_SIZE)
            {
                return false;//读的数据个数不能超过 BUFFER_SIZE
            }
            WRDataBlock dataBlock = new WRDataBlock
            {
                address = ss,
                original_address = address_list,
                dat_count = n,
                data = new short[BUFFER_SIZE]
            };
            IntPtr buff;
            try
            {
                buff = Marshal.AllocHGlobal(Marshal.SizeOf(dataBlock)); 
                Marshal.StructureToPtr(dataBlock, buff, false);
           }
            catch
            {//out of memory
                return false;
            }
            if (Win32API.PostThreadMessage(_thread.ThreadId, WorkThread.TM_READ_RANDOM, buff, IntPtr.Zero))
            {
                return true;
            }
            else
            {
                Marshal.FreeHGlobal(buff);
                return false;
            }
        }
        #endregion "ReadRandom"
        #region "ReadBlock"
        /// <summary>
        /// 异步单次读数据块
        /// </summary>
        /// <param name="start_address">欲读数据块的PLC起始地址</param>
        /// <param name="count">数据个数（以word为单位）</param>
        /// <returns>PLC未连接，或数据个数超过 BUFFER_SIZE， 返回 false </returns>
        public bool ReadBlock (string start_address, int count)
        {
            if (!Connected) return false;
            if ( count > BUFFER_SIZE )
            {
                return false;//读的数据个数不能超过BUFFER_SIZE
            }
            WRDataBlock dataBlock = new WRDataBlock
            {
                address = start_address,
                original_address = start_address,
                dat_count = (short)count,
                data = new short[BUFFER_SIZE]
            };
            IntPtr buff;
            try
            {
                buff = Marshal.AllocHGlobal(Marshal.SizeOf(dataBlock));
                Marshal.StructureToPtr(dataBlock, buff, false);
            }
            catch
            {
                //out of memory
                return false;
            }
            if (Win32API.PostThreadMessage(_thread.ThreadId, WorkThread.TM_READ_BLOCK, buff, (IntPtr)count))
            {
                return true;
            }
            else
            {
                Marshal.FreeHGlobal(buff);
                return false;
            }
        }
        #endregion "ReadBlock"
        #region "WriteRandom"
        /// <summary>
        /// 异步随机写数据
        /// </summary>
        /// <param name="address_list">PLC地址列表</param>
        /// <param name="Values">欲写入的值。所有的值必须有明确的数据类型</param>
        /// <returns>PLC未连接，数据个数与地址个数不相等，数据个数超过 BUFFER_SIZE，均返回 FALSE </returns>
        public bool WriteRandom(string address_list, object[] Values)
        {
            if (!Connected) return false;
            string[] ss = address_list.Split(',');
            if (ss.Length != Values.Length) return false;
            string addr = "";
            short[] dat = new short[BUFFER_SIZE];
            int ndx = 0;
            try
            {
                for (int i = 0; i < ss.Length; i++)
                {
                    if (ndx >= BUFFER_SIZE) return false;
                    addr += ss[i] + "\n";
                    Type t = Values[i].GetType();
                    if (typeof(bool) == t)
                    {
                        dat[ndx] = (short)(((bool)Values[i]) ? 1 : 0);
                        ndx++;
                    }
                    else
                    if (typeof(short) == t || typeof(ushort) == t)
                    {
                        dat[ndx] = (short)Values[ndx];
                        ndx++;
                    }
                    else
                    {
                        string s_addr;
                        int n_addr;
                        byte b_base;
                        PLC_Tag.SplitAddress(ss[i], out s_addr, out n_addr, out b_base);
                        if (typeof(Int32) == t || typeof(UInt32) == t)
                        {
                            addr += s_addr + Convert.ToString(n_addr + 1, b_base) + "\n";
                            short[] res = ToPLCWords((Int32)Values[i]);
                            res.CopyTo(dat, ndx);
                            ndx += 2;
                        }
                        else
                        if (typeof(float) == t || typeof(double) == t)
                        {
                            addr += s_addr + Convert.ToString(n_addr + 1, b_base) + "\n";
                            short[] res = ToPLCWords((float)Values[i]);
                            res.CopyTo(dat, ndx);
                            ndx += 2;
                        }
                        else
                        if (typeof(string) == t)
                        {
                            short[] res = ToPLCWords((string)Values[i]);
                            res.CopyTo(dat, ndx);
                            int k = res.Length;
                            ndx += k;
                            for (int j = 1; j < k; j++)
                            {
                                addr += s_addr + Convert.ToString(n_addr + 1, b_base) + "\n";
                            }
                        }
                        else
                            return false;
                    }
                }
            }
            catch
            {
                addr = "";
            }
            if (addr.Length == 0) return false;
            
            WRDataBlock dat_block = new WRDataBlock
            {
                address = addr.TrimEnd('\n'),
                original_address = address_list,
                dat_count = (short)ndx,
                data = dat,
            };
            IntPtr buff;
            try
            {
                buff = Marshal.AllocHGlobal(Marshal.SizeOf(dat_block));
                Marshal.StructureToPtr(dat_block, buff, false);
            }
            catch
            {
                return false;
            }
          
            if (Win32API.PostThreadMessage(_thread.ThreadId, WorkThread.TM_WRITE_RANDOM, buff, IntPtr.Zero))
            {
                return true;
            }
            else
            {
                Marshal.FreeHGlobal(buff);
                return false;
            }
        }
        #endregion "WriteRandom"
        #region "WriteBlock"
        /// <summary>
        /// 异步写数据块，以 word 为单位写入 PLC
        /// </summary>
        /// <param name="start_address">欲写数据块的起始地址</param>
        /// <param name="Values">写入的值</param>
        /// <returns>PLC未连接，或数据个数超过 BUFFER_SIZE，返回 FALSE </returns>
        public bool WriteBlock(string start_address, short[] Values)
        {
            if (!Connected) return false;
            if (Values.Length > BUFFER_SIZE) return false;
            WRDataBlock dat_block = new WRDataBlock
            {
                address = start_address,
                original_address = start_address,
                dat_count = (short)Values.Length,
                data = new short[BUFFER_SIZE],
            };
            Values.CopyTo(dat_block.data, 0);
            IntPtr buff;
            try
            {
                buff = Marshal.AllocHGlobal(Marshal.SizeOf(dat_block));
//                Marshal.StructureToPtr<WRDataBlock>(dat_block, buff, false);
                Marshal.StructureToPtr(dat_block, buff, false);
            }
            catch
            {
                return false;
            }
            if (Win32API.PostThreadMessage(_thread.ThreadId, WorkThread.TM_WRITE_BLOCK, buff, (IntPtr)Values.Length))
            {
                return true;
            }
            else
            {
                Marshal.FreeHGlobal(buff);
                return false;
            }
        }
        #endregion "WriteBlock"
        #region "GetPLCInfo"
        /// <summary>
        /// 从注册表读取 PLC 有关信息
        /// </summary>
        /// <returns>返回 PLCSimpleInfo 数组</returns>
        public static PLCSimpleInfo[] GetPLCInfo()
        {
            //HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\MITSUBISHI\SWnD5-ACT\COMMUTL
            RegistryKey key = Registry.LocalMachine;
            RegistryKey sk = key.OpenSubKey("SOFTWARE\\WOW6432Node\\MITSUBISHI\\SWnD5-ACT\\COMMUTL");
            if (null == sk)
            {
                key.Close();
                return null;
            }
            long r = 0;
            PLCSimpleInfo[] ret = null;
            try
            {
                if (!long.TryParse(sk.GetValue("LogicalStNoCount").ToString(), out r))
                {
                    r = -1;
                }
            }
            catch
            {
                r = -1;
            }
            if (r > 0)
            {
                if (r > 0)
                {
                    byte[] ab = (byte[])sk.GetValue("LogicalStNoData");
                    ret = new PLCSimpleInfo[r];
                    short no = 0;
                    string kname;
                    for (long i = 0; i < r; i++)
                    {
                        no = (short)(ab[2 * i] + (ab[2 * i + 1] << 8));
                        kname = string.Format("LogicalStNo_{0:D4}\\UTL", no);
                        RegistryKey tmpkey = sk.OpenSubKey(kname);
                        if (null != tmpkey)
                        {
                            ret[i] = new PLCSimpleInfo
                            {
                                LogicalNo = no
                            };
                            try
                            {
                                ret[i].Comment = tmpkey.GetValue("Comment").ToString();
                            }
                            catch
                            {
                                ret[i].Comment = "";
                            }
                            tmpkey.Close();
                        }
                    }
                }
            }
            sk.Close();
            key.Close();
            return ret;
        }
        #endregion "GetPLCInfo"

        #region "EnumPLCTags"
        /// <summary>
        /// 枚举窗体中所有 PLC 变量
        /// </summary>
        /// <param name="form">MXObject的父窗体</param>
        /// <param name="control">被枚举的窗体</param>
        /// <param name="plcs">按 logical number 创建MXObject， 并保存到plcs中</param>
        public static void EnumPLCTags(Form form, Control control, List<MXObject> plcs)
        {
            if (control.HasChildren)
            {
                string[] ss;
                short n = 0;
                bool flag;
                foreach (Control ctrl in control.Controls)
                {
                    if (null != ctrl.Tag)
                    {
                        ss = ctrl.Tag.ToString().ToUpper().Split(':');//format--logicalno:datatype:address:length
                        flag = ss.Length >= 3;
                        if (flag)
                        {
                            flag = short.TryParse(ss[0], out n);//n is logical number
                        }
                        if (flag)
                        {
                            Tag_DataType dType;
                            int len = -1;
                            switch (ss[1])
                            {
                                case "B":
                                    dType = Tag_DataType.BIT;
                                    break;
                                case "I2":
                                    dType = Tag_DataType.INT16;
                                    break;
                                case "U2":
                                    dType = Tag_DataType.UINT16;
                                    break;
                                case "I4":
                                    dType = Tag_DataType.INT32;
                                    break;
                                case "U4":
                                    dType = Tag_DataType.UINT32;
                                    break;
                                case "F":
                                    dType = Tag_DataType.FLOAT;
                                    break;
                                case "S":
                                    dType = Tag_DataType.STRING;
                                    if (ss.Length > 3)
                                    {
                                        flag = int.TryParse(ss[3], out len);
                                        if (!flag) len = 32;
                                    }
                                    else
                                    {
                                        len = 32;
                                    }
                                    break;
                                default:
                                    dType = Tag_DataType.UNKNOWN;
                                    break;
                            }
                            if (dType != Tag_DataType.UNKNOWN)
                            {
                                MXObject plc = null;
                                foreach (MXObject _plc in plcs)
                                {
                                    if (_plc.LogicalNo == n)
                                    {
                                        plc = _plc;
                                        break;
                                    }
                                }
                                if (plc == null)
                                {
                                    plc = new MXObject(form, n, "");
                                    plcs.Add(plc);
                                }
                                PLC_Tag tag = null;
                                foreach (PLC_Tag _tag in plc.TagList4Random)
                                {
                                    if (_tag.Address == ss[2] && _tag.DataType == dType)
                                    {
                                        tag = _tag;
                                        break;
                                    }
                                }
                                if (tag == null)
                                {
                                    tag = plc.AddTag(ss[2], dType, len);
                                }
                                if (tag != null)
                                    tag.associatedCtrls.Add(ctrl);
                            }
                        }
                    }
                    EnumPLCTags(form, ctrl, plcs);
                }
            }
        }
        #endregion "EnumPLCTags"
        /// <summary>
        /// 将两个word转换为32位整数
        /// </summary>
        /// <param name="low_word">低16位</param>
        /// <param name="hi_word">高16位</param>
        /// <returns> 32位整数 </returns>
        public static int ParseInt32(short low_word, short hi_word)
        {
            return ((Int32)low_word & 0x0ffff) + ((Int32)hi_word << 16);
        }
        /// <summary>
        /// 将两个word转换为单精度浮点数
        /// </summary>
        /// <param name="low_word">低16位</param>
        /// <param name="hi_word">高16位</param>
        /// <returns> 单精度浮点数 </returns>
        public static float ParseFloat(short low_word, short hi_word)
        {
            byte[] b = BitConverter.GetBytes(ParseInt32(low_word, hi_word));
            return BitConverter.ToSingle(b, 0);
        }
        /// <summary>
        /// 将连续的 count 个words转换为字符串
        /// </summary>
        /// <param name="dat">word 数组</param>
        /// <param name="index">起始索引号</param>
        /// <param name="count">short元素个数</param>
        /// <returns>字符串，长度不超过 count * 2</returns>
        public static string ParseString(short[] dat, int index, int count = 16)
        {
            string s = "";
            int ndx;
            for (int i = 0; i < count; i++)
            {
                ndx = index + i;
                if (ndx >= dat.Length) break;
                try
                {
                    byte[] b = BitConverter.GetBytes(dat[ndx]);
                    s += System.Text.Encoding.ASCII.GetString(b);
                    if (b[0] == 0 || b[1] == 0) break;
                }
                catch
                {
                    return null;
                }
            }
            return s.TrimEnd('\0');
        }
        /// <summary>
        /// 将字符串转为short数组，字符串长度不能超过32
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <returns>word数组</returns>
        public static short[] ToPLCWords(string s)
        {
            byte[] b = System.Text.Encoding.ASCII.GetBytes(s);
            int k = (b.Length + 2) / 2;
            short[] ret = new short[k];
            int ndx;
            for (int i = 0; i < k; i++)
            {
                ndx = 2 * i;
                if (ndx + 1 < b.Length)
                {
                    ret[i] = (short)(((short)b[ndx] & 0x0ff) + ((short)b[ndx + 1] << 8));
                }
                else
                if (ndx < b.Length)
                {
                    ret[i] = (short)((short)b[ndx] & 0x0ff);
                }
                else
                    ret[i] = 0;
            }
            return ret;
        }
        /// <summary>
        /// 将单精度浮点数转换为word数组
        /// </summary>
        /// <param name="value">单精度浮点数</param>
        /// <returns>长度为2的word数组</returns>
        public static short[] ToPLCWords(float value)
        {
            short[] ret = new short[2];
            byte[] b = BitConverter.GetBytes(value);
            ret[0] = (short)((0x0ff & b[0]) + (b[1] << 8));
            ret[1] = (short)((0x0ff & b[2]) + (b[3] << 8));
            return ret;
        }
        /// <summary>
        /// 将32位整数转换为word数组
        /// </summary>
        /// <param name="value">32位整数</param>
        /// <returns>长度为2的word数组</returns>
        public static short[] ToPLCWords(int value)
        {
            short[] ret = new short[2];
            byte[] b = BitConverter.GetBytes(value);
            ret[0] = (short)((0x0ff & b[0]) + (b[1] << 8));
            ret[1] = (short)((0x0ff & b[2]) + (b[3] << 8));
            return ret;
        }
    }

    #region "class MyThread"
    abstract public class MyThread
    {
        public Thread threadObj = null;
        public volatile bool terminated = false;
        public volatile UInt32 ThreadId = UInt32.MaxValue;//-1

        abstract public void Execute();
        abstract public void Execute(object parameter);

        public static byte[] StrToByteArray(string str, int len)
        {
            byte[] strBytes = Encoding.Default.GetBytes(str);
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = (i < strBytes.Length) ? strBytes[i] : (byte)0;
            }
            return result;
        }

        ~MyThread()
        {
            if (null != threadObj)
            {
                Stop(true);
            }
        }
        public void Start()
        {
            if (null == threadObj)
            {
                threadObj = new Thread(new ThreadStart(Execute));
                threadObj.SetApartmentState(ApartmentState.STA);
                threadObj.IsBackground = true;
                threadObj.Start();
            }
        }

        public void Start(object parameter)
        {
            if (null == threadObj)
            {
                threadObj = new Thread(new ParameterizedThreadStart(Execute));
                //threadObj.SetApartmentState(ApartmentState.STA);
                threadObj.IsBackground = true;
                threadObj.Start(parameter);
            }
        }

        public void Stop(bool forced = false)
        {
            if (null != threadObj)
            {
                if (forced)
                {
                    threadObj.Abort();
                }
                else
                {
                    terminated = true;
                }
            }
        }
    }
    #endregion "class MyThread"
    #region "class WorkThread"
    class WorkThread: MyThread
    {
        public const Int32 TM_THREAD_FINISHED = Win32API.WM_USER + 10;
        public const Int32 TM_CONNECTION = Win32API.WM_USER + 11;
        public const Int32 TM_NORMAL_READ = TM_CONNECTION + 1;
        public const Int32 TM_NORMAL_WRITE = TM_NORMAL_READ + 1;
        public const Int32 TM_READ_RANDOM = TM_NORMAL_WRITE + 1;
        public const Int32 TM_WRITE_RANDOM = TM_READ_RANDOM + 1;
        public const Int32 TM_READ_BLOCK = TM_WRITE_RANDOM + 1;
        public const Int32 TM_WRITE_BLOCK = TM_READ_BLOCK + 1;
        public const Int32 CONNECTION_BREAK = -1;

        private IntPtr _owner = IntPtr.Zero;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="hWnd">拥有者控件的HANDLE(HWND)</param>
        public WorkThread(IntPtr hWnd)
        {
            _owner = hWnd;
        
        }
        public override void Execute()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 线程函数
        /// </summary>
        /// <param name="parameter">线程创建者传入的参数，此处为一个MXObject对象</param>
        public override void Execute(object parameter)
        {
            //throw new NotImplementedException();
            if (!(parameter is MXObject))
                return;

            ThreadId = Win32API.GetCurrentThreadId();

            MXObject mxobj = parameter as MXObject;

            ActUtlTypeLib.ActUtlTypeClass autc = new ActUtlTypeLib.ActUtlTypeClass
            {
                ActLogicalStationNumber = mxobj.LogicalNo,
                ActPassword = mxobj.Password
            };
            int conn_flag = -1;
            MSG msg = new MSG();
            bool op_flag;
            while (!(mxobj.StopEvent.WaitOne(0)))
            {
                if (0 == conn_flag)
                {
                    //读数据
                    if(mxobj.AddrList4Random.Length > 0)
                    {
                        short[] dat = ReadRandom(autc, mxobj.AddrList4Random, mxobj.WordCounts4Random);
                        if (dat != null && dat.Length > 0)
                        {
                            ResolveData(dat, mxobj.TagList4Random);
                            Win32API.SendMessage(_owner, TM_NORMAL_READ, IntPtr.Zero, IntPtr.Zero);
                        }
                        else
                        {
                            autc.Close();
                            conn_flag = -1;

                            Win32API.SendMessage(_owner, TM_CONNECTION,
                                (IntPtr)CONNECTION_BREAK,
                                (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                        }
                    }
                    if (mxobj.TagList4Block.Count > 0)
                    {
                        foreach(BlockTagInfo _info in mxobj.TagList4Block)
                        {
                            if (0 != conn_flag) break;
                            if (mxobj.StopEvent.WaitOne(0)) goto finally_proc;

                            short[] dat = ReadBlock(autc, _info.Address, _info.Count);
                            if ( null != dat && dat.Length > 0)
                            {
                                ResolveBlockData(dat, _info.Address, _info.Tags);
                                //Win32API.SendMessage(_owner, TM_NORMAL_READ, (IntPtr)1, IntPtr.Zero);
                            }
                            else
                            {
                                autc.Close();
                                conn_flag = -1;
                                Win32API.SendMessage(_owner, TM_CONNECTION,
                                    (IntPtr)CONNECTION_BREAK,
                                    (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                            }
                        }
                        if (0 == conn_flag)
                        {
                            Win32API.SendMessage(_owner, TM_NORMAL_READ, (IntPtr)1, IntPtr.Zero);
                        }
                    }
                    //处理外部发来的读写请求
                    while (0 != Win32API.PeekMessage(ref msg, IntPtr.Zero, 0, 0, Win32API.PM_REMOVE))
                    {
                        if (mxobj.StopEvent.WaitOne(0)) goto finally_proc;
                        op_flag = 0 == conn_flag;
                        WRDataBlock wdb;
                        switch (msg.message)
                        {
                            case TM_READ_RANDOM:
                                try
                                {
                                    short[] dat = null;
                                    if (op_flag)
                                    {
                                        //wdb = Marshal.PtrToStructure<WRDataBlock>(msg.wParam);//vs2017
                                        wdb = (WRDataBlock)Marshal.PtrToStructure(msg.wParam, typeof(WRDataBlock));
                                        dat = ReadRandom(autc, wdb.address, wdb.dat_count);
                                    }
                                    op_flag = op_flag && dat != null && dat.Length > 0;
                                    if (op_flag)
                                    {
                                        IntPtr tmp = msg.wParam;
                                        for (int i = 0; i < dat.Length; i++)
                                        {
                                            Marshal.WriteInt16(tmp, (char)dat[i]);
                                            tmp += 2;
                                        }
                                        Win32API.SendMessage(_owner, TM_READ_RANDOM, msg.wParam, msg.lParam);
                                    }
                                    else
                                    {
                                        Win32API.SendMessage(_owner, TM_READ_RANDOM,
                                            msg.wParam,
                                            (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                                        if (conn_flag == 0)
                                        {
                                            autc.Close();
                                            conn_flag = -1;
                                            Win32API.SendMessage(_owner, TM_CONNECTION,
                                                (IntPtr)CONNECTION_BREAK,
                                                (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                                        }
                                    }
                                }
                                catch
                                {
                                    Win32API.SendMessage(_owner, TM_READ_RANDOM, msg.wParam, (IntPtr)(-3));
                                }
                                break;
                            case TM_READ_BLOCK:
                                try
                                {
                                    short[] dat = null;
                                    if (op_flag)
                                    {
                                        //wdb = Marshal.PtrToStructure<WRDataBlock>(msg.wParam);//vs2017
                                        wdb = (WRDataBlock)Marshal.PtrToStructure(msg.wParam, typeof(WRDataBlock));
                                        dat = ReadBlock(autc, wdb.address, wdb.dat_count);
                                    }
                                    op_flag = op_flag && dat != null && dat.Length > 0;
                                    if (op_flag)
                                    {
                                        IntPtr tmp = msg.wParam;
                                        for (int i = 0; i < dat.Length; i++)
                                        {
                                            Marshal.WriteInt16(tmp, (char)dat[i]);
                                            tmp += 2;
                                        }
                                        Win32API.SendMessage(_owner, TM_READ_BLOCK, msg.wParam, msg.lParam);
                                    }
                                    else
                                    {
                                        Win32API.SendMessage(_owner, TM_READ_BLOCK,
                                            msg.wParam,
                                            (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                                        if (conn_flag == 0)
                                        {
                                            autc.Close();
                                            conn_flag = -1;
                                            Win32API.SendMessage(_owner, TM_CONNECTION,
                                                (IntPtr)CONNECTION_BREAK,
                                                (null != dat) ? ((IntPtr)(-1)) : ((IntPtr)(-2)));
                                        }
                                    }
                                }
                                catch
                                {
                                    Win32API.SendMessage(_owner, TM_READ_BLOCK, msg.wParam, (IntPtr)(-3));
                                }
                                break;
                            case TM_WRITE_RANDOM:
                                try
                                {
                                    int r = -1;
                                    if (op_flag)
                                    {
                                        //wdb = Marshal.PtrToStructure<WRDataBlock>(msg.wParam);//vs2017
                                        wdb = (WRDataBlock)Marshal.PtrToStructure(msg.wParam, typeof(WRDataBlock));
                                        short[] dat = new short[wdb.dat_count];
                                        Array.Copy(wdb.data, dat, wdb.dat_count);
                                        r = WriteRandom(autc, wdb.address, dat);
                                    }
                                    op_flag = op_flag && (r == 0);
                                    if (op_flag)
                                    {
                                        Win32API.SendMessage(_owner, TM_WRITE_RANDOM, msg.wParam, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Win32API.SendMessage(_owner, TM_WRITE_RANDOM, msg.wParam, (IntPtr)r);
                                        if (0 == conn_flag)
                                        {
                                            conn_flag = -1;
                                            autc.Close();
                                            Win32API.SendMessage(_owner, TM_CONNECTION,
                                                (IntPtr)CONNECTION_BREAK, (IntPtr)r);
                                        }
                                    }
                                }
                                catch
                                {
                                    Win32API.SendMessage(_owner, TM_WRITE_RANDOM, msg.wParam, (IntPtr)(-3));
                                }
                                break;
                            case TM_WRITE_BLOCK:
                                try
                                {
                                    int r = -1;
                                    if (op_flag)
                                    {
                                        //wdb = Marshal.PtrToStructure<WRDataBlock>(msg.wParam);//vs2017
                                        wdb = (WRDataBlock)Marshal.PtrToStructure(msg.wParam, typeof(WRDataBlock));
                                        short[] dat = new short[wdb.dat_count];
                                        Array.Copy(wdb.data, dat, wdb.dat_count);
                                        r = WriteBlock(autc, wdb.address, dat);
                                    }
                                    op_flag = op_flag && (r == 0);
                                    if (op_flag)
                                    {
                                        Win32API.SendMessage(_owner, TM_WRITE_BLOCK, msg.wParam, IntPtr.Zero);
                                    }
                                    else
                                    {
                                        Win32API.SendMessage(_owner, TM_WRITE_BLOCK, msg.wParam, (IntPtr)r);
                                        if (0 == conn_flag)
                                        {
                                            conn_flag = -1;
                                            autc.Close();
                                            Win32API.SendMessage(_owner, TM_CONNECTION,
                                                (IntPtr)CONNECTION_BREAK, (IntPtr)r);
                                        }
                                    }
                                }
                                catch
                                {
                                    Win32API.SendMessage(_owner, TM_WRITE_BLOCK, msg.wParam, (IntPtr)(-3));
                                }
                                break;
                            default:
                                break;
                        }//end switch
                    }//end while (PeekMessage(...)
                }
                else
                {
                    //连接plc
                    try
                    {
                        conn_flag = autc.Open();
                    }
                    catch
                    {
                        conn_flag = -1;
                    }
                    if (IntPtr.Zero != _owner)
                    {
                        Win32API.SendMessage(_owner, TM_CONNECTION, (IntPtr)conn_flag, IntPtr.Zero);
                    }
                    if (0 != conn_flag)
                    {
                        //连接失败， 1000ms后重试
                        if (mxobj.StopEvent.WaitOne(1000))
                            goto finally_proc;
                    }
                }
            }// end main loop

        finally_proc:
            ThreadId = UInt32.MaxValue;
            if (0 == conn_flag)
            {
                autc.Close();
                Win32API.SendMessage(_owner, TM_CONNECTION, (IntPtr)(-2), IntPtr.Zero);
            }
            autc = null;
            Win32API.PostMessage(_owner, TM_THREAD_FINISHED, IntPtr.Zero, IntPtr.Zero);
        }
        #region "ReadRandom"
        /// <summary>
        /// 随机读数据
        /// </summary>
        /// <param name="act">ActUtlTypeClass</param>
        /// <param name="addrs">以\n分隔的地址列表</param>
        /// <param name="number">tag个数</param>
        /// <returns>返回读取的数据，读取失败返回空数组</returns>
        private short[] ReadRandom(ActUtlTypeLib.ActUtlTypeClass act, string addrs, int number)
        {
            try
            {
                short[] data = new short[number];
                int r = act.ReadDeviceRandom2(addrs, number, out data[0]);
                if (0 != r)
                    return new short[] { };
                return data;
            }
            catch
            {
                return null;
            }
        }
        #endregion "ReadRandom"
        #region "ReadBlock"
        /// <summary>
        /// 读取连续的数据
        /// </summary>
        /// <param name="act">ActUtlTypeClass</param>
        /// <param name="address">起始地址</param>
        /// <param name="number">数据个数</param>
        /// <returns>返回读取的数据，读取失败返回空数组</returns>
        private short[] ReadBlock(ActUtlTypeLib.ActUtlTypeClass act, string address, int number)
        {
            try
            {
                short[] data = new short[number];
                int r = act.ReadDeviceBlock2(address, number, out data[0]);
                if (0 != r)
                    return new short[] { };
                return data;
            }
            catch
            {
                return null;
            }
        }
        #endregion "ReadBlock"
        #region "WriteRandom"
        private int WriteRandom(ActUtlTypeLib.ActUtlTypeClass act, string addr_list, short[] Values)
        {
            int r;
            try
            {
                r = act.WriteDeviceRandom2(addr_list, Values.Length, ref Values[0]);
            }
            catch
            {
                r = -1;
            }
            return r;
        }
        #endregion "WriteRandom"
        #region "WriteBlock"
        private int WriteBlock(ActUtlTypeLib.ActUtlTypeClass act, string addr_start, short[] Values)
        {
            int r;
            try
            {
                r = act.WriteDeviceBlock2(addr_start, Values.Length, ref Values[0]);
            }
            catch
            {
                r = -1;
            }
            return r;
        }
        #endregion "WriteBlock"
        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="dat">原始数据</param>
        /// <param name="start_addr">块的首地址</param>
        /// <param name="tags">plc tag清单</param>
        /// <returns>解析数据的个数</returns>
        private int ResolveBlockData(short[] dat, string start_addr, List<PLC_Tag> tags)
        {
            int first_addr;
            string s_addr;
            byte b_base;
            int ndx = 0, k = 0;
            PLC_Tag.SplitAddress(start_addr, out s_addr, out first_addr, out b_base);
            
            foreach(PLC_Tag _tag in tags)
            {
                ndx = _tag.NumPart - first_addr;
                if (ndx >= 0 && ndx < dat.Length)
                {
                    _tag.Quality = PLC_Tag.QUALITY_GOOD;
                    switch (_tag.DataType)
                    {
                        case Tag_DataType.BIT:
                            _tag.Value.bValue = dat[ndx] == 1;
                            break;
                        case Tag_DataType.INT16:
                            _tag.Value.i2Value = dat[ndx];
                            break;
                        case Tag_DataType.UINT16:
                            _tag.Value.u2Value = (UInt16)dat[ndx];
                            break;
                        case Tag_DataType.INT32:
                            if (ndx + 1 < dat.Length)
                            {
                                _tag.Value.i4Value = MXObject.ParseInt32(dat[ndx], dat[ndx + 1]);
                            }
                            else
                                _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                        case Tag_DataType.UINT32:
                            if (ndx + 1 < dat.Length)
                            {
                                _tag.Value.u4Value = (UInt32)(MXObject.ParseInt32(dat[ndx], dat[ndx + 1]));
                            }
                            else
                                _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                        case Tag_DataType.FLOAT:
                            if (ndx + 1 < dat.Length)
                            {
                                _tag.Value.fValue = MXObject.ParseFloat(dat[ndx], dat[ndx + 1]);
                            }
                            else
                                _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                        case Tag_DataType.STRING:
                            int m = (_tag.DataLength + 1) / 2;
                            if (ndx + m <= dat.Length)
                            {
                                _tag.szValue = MXObject.ParseString(dat, ndx, m);
                            }
                            else
                                _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                        default:
                            _tag.Value.u4Value = 0;
                            _tag.szValue = "";
                            _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                    }
                    k++;
                }
                else
                    _tag.Quality = PLC_Tag.QUALITY_BAD;
            }
            return k;
        }
        public int ResolveData(short[] data, List<PLC_Tag> tags)
        {
            int n = 0, k = 0;

            foreach (PLC_Tag _tag in tags)
            {
                if (n < data.Length)
                {
                    _tag.Quality = PLC_Tag.QUALITY_GOOD;
                    switch (_tag.DataType)
                    {
                        case Tag_DataType.BIT:
                            _tag.Value.bValue = data[n] == 1;
                            n++;
                            break;
                        case Tag_DataType.INT16:
                            _tag.Value.i2Value = data[n];
                            n++;
                            break;
                        case Tag_DataType.UINT16:
                            _tag.Value.u2Value = (UInt16)data[n];
                            n++;
                            break;
                        case Tag_DataType.INT32:
                            if (n + 1 < data.Length)
                            {
                                _tag.Value.i4Value = MXObject.ParseInt32(data[n], data[n + 1]);
                            }
                            else _tag.Quality = PLC_Tag.QUALITY_BAD;
                            n += 2;
                            break;
                        case Tag_DataType.UINT32:
                            if (n + 1 < data.Length)
                            {
                                _tag.Value.u4Value = (UInt32)(MXObject.ParseInt32(data[n], data[n + 1]));
                            }
                            else _tag.Quality = PLC_Tag.QUALITY_BAD;
                            n += 2;
                            break;
                        case Tag_DataType.FLOAT:
                            if (n + 1 < data.Length)
                            {
                                _tag.Value.fValue = MXObject.ParseFloat(data[n], data[n + 1]);
                            }
                            else _tag.Quality = PLC_Tag.QUALITY_BAD;
                            n += 2;
                            break;
                        case Tag_DataType.STRING:
                            int m = (_tag.DataLength + 1) / 2;
                            if (n + m <= data.Length)
                            {
                                _tag.szValue = MXObject.ParseString(data, n, m);
                            }
                            else _tag.Quality = PLC_Tag.QUALITY_BAD;
                            n += m;
                            break;
                        default:
                            _tag.Value.u4Value = 0;
                            _tag.szValue = "";
                            _tag.Quality = PLC_Tag.QUALITY_BAD;
                            break;
                    }
                    if (_tag.Quality == PLC_Tag.QUALITY_GOOD) k++;
                }
                else
                {
                    _tag.Quality = PLC_Tag.QUALITY_BAD;
                }
            }
            return k;
        }
    }
    #endregion "class WorkThread"
}