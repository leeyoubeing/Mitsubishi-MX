using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PLCDataAccess;

namespace DataAccess
{
    public partial class Form1 : Form
    {
        private Button _btn_connect = new Button();
        //private WorkThread aThread = null;
        private List<WorkThread> threadList = new List<WorkThread>();
        private List<MXObject> plcList;
        private bool threadsRunning = false;

        public Form1()
        {
            InitializeComponent();
            _btn_connect.Visible = false;
            _btn_connect.TabStop = false;
            btnStart.Image = (Image)Properties.Resources.ResourceManager.GetObject("play");
            button1.Image = (Image)Properties.Resources.ResourceManager.GetObject("refresh");

            plcList = new List<MXObject>();

            loadMxPLCs();
            MXObject.EnumPLCTags(this, this, plcList);
            foreach(MXObject plc in plcList)
            {
                comboBox1.Items.Add(plc.LogicalNo.ToString());
            }
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            List<PLC_Tag> D40_50List = new List<PLC_Tag>();
            for(int i = 0; i <= 10; i++)
            {
                PLC_Tag atag = new PLC_Tag();
                atag.Address = "D" + (i + 40).ToString();
                atag.DataType = Tag_DataType.INT16;
                D40_50List.Add(atag);

            }
            plcList[0].AddBlock("D40", 11, D40_50List);

            foreach(MXObject obj in plcList)
            {
                obj.OnConnectPLC += OnConnect;
                obj.OnUpdateTagValue += OnUpdateTagValue;
                obj.OnReadRandomComplete += OnReadRandom;
                obj.OnReadBlockComplete += OnReadBlock;
                obj.OnWriteRandomComplete += OnWriteRandom;
                obj.OnWriteBlockComplete += OnWriteBlock;
            }
        }
        private void OnConnect(MXObject plc, int flag, int info)
        {
            string s;
            if (flag == 0)
            {
                s = $"PLC(logicalNO={plc.LogicalNo})连接成功。";
            }
            else
            if (flag == -1)
            {
                s = $"PLC(logicalNO={plc.LogicalNo})读写异常，连接中断。";
            }
            else
            if (flag == -2)
            {
                s = $"PLC(logicalNO={plc.LogicalNo})连接关闭。";
            }
            else
            {
                s = $"PLC(logicalNO={plc.LogicalNo})连接失败。";
            }
            
            ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
            item.SubItems.Add(s);
        }
        private void OnUpdateTagValue(MXObject plc, int list_type)
        {
            if (0 == list_type)
            {
                foreach (PLC_Tag _tag in plc.TagList4Random)
                {
                    foreach (Control ctrl in _tag.associatedCtrls)
                    {
                        if (ctrl is TextBox)
                        {
                            (ctrl as TextBox).Text = _tag.ToString();
                        }
                    }
                }
            }
            else
            if (1 == list_type)
            {
                string s = "";
                foreach(BlockTagInfo _bi in plc.TagList4Block)
                {
                    foreach(PLC_Tag _tag in _bi.Tags)
                    {
                        s += _tag.ToString() + ",";
                    }
                }
                textBox17.Text = s.TrimEnd(',') ;
            }
        }
        private void OnReadRandom(MXObject Sender, string addr_list, short[] dat)
        {
            if (dat == null)
            {
                ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
                item.SubItems.Add(String.Format("随机读数据失败（PLC logicalNO={0}）", Sender.LogicalNo));
            }
            else
            {
                ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
                item.SubItems.Add(String.Format("随机读数据成功（PLC logicalNO={0}）", Sender.LogicalNo));
                string s = "";
                foreach(short v in dat)
                {
                    s += v.ToString() + ",";
                }
                textBox16.Text = s.TrimEnd(',');
            }
        }
        private void OnReadBlock(MXObject Sender, string start_addr, short[] dat)
        {
            if (dat == null)
            {
                ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
                item.SubItems.Add(String.Format("读数据块失败（PLC logicalNO={0}）", Sender.LogicalNo));
            }
            else
            {
                ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
                item.SubItems.Add(String.Format("读数据块成功（PLC logicalNO={0}）", Sender.LogicalNo));
                string s = "";
                foreach (short v in dat)
                {
                    s += v.ToString() + ",";
                }
                textBox15.Text = s.TrimEnd(',');
            }
        }
        private void OnWriteRandom(MXObject Sender, string addr_list, bool flag)
        {
            string s;
            if (flag)
            {
                s = string.Format("随机写数据成功(PLC logicalNo={0})", Sender.LogicalNo);
            }
            else
            {
                s = string.Format("随机写数据失败(PLC logicalNo={0})", Sender.LogicalNo);
            }
            ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
            item.SubItems.Add(s);
        }
        private void OnWriteBlock(MXObject Sender, string start_addr, bool flag)
        {
            string s;
            if (flag)
            {
                s = string.Format("写数据块成功(PLC logicalNo={0})", Sender.LogicalNo);
            }
            else
            {
                s = string.Format("写数据块失败(PLC logicalNo={0})", Sender.LogicalNo);
            }
            ListViewItem item = listView2.Items.Insert(0, DateTime.Now.ToString());
            item.SubItems.Add(s);
        }
        private void loadMxPLCs()
        {
            listView1.Items.Clear();
            PLCSimpleInfo[] plcs = MXObject.GetPLCInfo();
            if (plcs != null)
            {
                foreach (PLCSimpleInfo plc in plcs)
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = plc.LogicalNo.ToString();
                    item.SubItems.Add(plc.Comment);
                    item.ImageIndex = 0;
                    listView1.Items.Add(item);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            loadMxPLCs();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //测试：启动一个线程
            /*            if (aThread == null)
                        {
                            PLC_MX plc = new PLC_MX("ActUtlType", 3, "");
                            aThread = new WorkThread(Handle);
                            aThread.start(plc);
                        }
                        else
                        {
                            aThread.stop();
                            aThread = null;
                        }*/

            //plcList[0].ReadRandom("D100,D200");
        }

        private void button3_Click(object sender, EventArgs e)
        {
/*            if (aThread != null && aThread.threadId != uint.MaxValue)
            {
                //Win32API.PostThreadMessage(thread.threadId, WorkThread.WM_THREAD_TEST, 993, 4);
                WRDB db = new WRDB();
                db.address = MyThread.StrToByteArray(textBox2.Text, WorkThread.DATA_COUNT * 5);
                db.dat_count = 0;
                db.data = new short[WorkThread.DATA_COUNT];
                IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf(db));
                Marshal.StructureToPtr(db, buf, true);
                Win32API.PostThreadMessage(aThread.threadId, WorkThread.WM_THREAD_READ, (int)buf, 0);
            }*/
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (threadsRunning)
            {
                //stop communication threads
                /*               foreach(WorkThread thread in threadList)
                               {
                                   thread.Stop();
                               }
                               threadList.Clear();
                               btnStart.Image =
                               (Image)Properties.Resources.ResourceManager.GetObject("play");

                               ListViewItem item = new ListViewItem(DateTime.Now.ToString());
                               item.SubItems.Add("停止所有通讯线程。");
                               listView2.Items.Add(item);
                               foreach(ListViewItem _item in listView1.Items)
                               {
                                   _item.ImageIndex = 0;
                               }*/
                foreach (MXObject obj in plcList)
                {
                    obj.Stop();
                }
                btnStart.Image =
                (Image)Properties.Resources.ResourceManager.GetObject("play");
                ListViewItem item = new ListViewItem(DateTime.Now.ToString());
                item.SubItems.Add("停止所有通讯线程。");
                listView2.Items.Insert(0, item);
            }
            else
            {
                //start communication threads
/*                int i = 0;
                foreach(PLC_MX _plc in plcList)
                {
                    WorkThread thread = new WorkThread(this.Handle);
                    _plc.Index = i;
                    i++;
                    threadList.Add(thread);
                    thread.start(_plc);
                }
                btnStart.Image =
                (Image)Properties.Resources.ResourceManager.GetObject("stop");

                ListViewItem item = new ListViewItem(DateTime.Now.ToString());
                item.SubItems.Add("启动所有通讯线程。");
                listView2.Items.Add(item);*/
                foreach(MXObject obj in plcList)
                {
                    obj.Start();
                }
                btnStart.Image =
                (Image)Properties.Resources.ResourceManager.GetObject("stop");
                ListViewItem item = new ListViewItem(DateTime.Now.ToString());
                item.SubItems.Add("启动所有通讯线程。");
                listView2.Items.Insert(0, item);
            }
            threadsRunning = !threadsRunning;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            //随机写数据
            int k = comboBox1.SelectedIndex;
            if (k >= 0)
            {
                string[] ss1 = textBox10.Text.Split(',');
                string[] ss2 = textBox11.Text.Split(',');
                if (ss1.Length != ss2.Length) return;
                object[] val = new object[ss2.Length];
                string s;
                for(int i = 0; i < ss2.Length; i++)
                {
                    s = ss2[i];
                    if (s.StartsWith("\"")) val[i] = s.Trim(new char[] { '"' });
                    else
                    if (s.StartsWith("i", true, null))
                        val[i] = Convert.ToInt32(s.Substring(1));
                    else
                    if (s.StartsWith("f", true, null))
                        val[i] = Convert.ToSingle(s.Substring(1));
                    else
                    if (s.StartsWith("c", true, null))
                        val[i] = Convert.ToInt16(s.Substring(1));
                    else
                        val[i] = Convert.ToInt16(s);
                }
                plcList[k].WriteRandom(textBox10.Text, val);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            //随机读
            int k = comboBox1.SelectedIndex;
            if (k >= 0)
            {
                plcList[k].ReadRandom(textBox12.Text);
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            //读数据块
            int k = comboBox1.SelectedIndex;
            int n = Convert.ToInt32(textBox2.Text);
            if (k >= 0 && n > 0)
            {
                plcList[k].ReadBlock(textBox1.Text, n);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //写数据块
            int k = comboBox1.SelectedIndex;
            if (k >=0 )
            {
                string[] ss = textBox14.Text.Split(',');
                short[] val = new short[ss.Length];
                for (int i = 0; i < ss.Length; i++)
                {
                    val[i] = Convert.ToInt16(ss[i]);
                }
                plcList[k].WriteBlock(textBox13.Text, val);
            }
        }
        /*
private void button4_Click(object sender, EventArgs e)
{
int n = comboBox1.SelectedIndex;
if (plcList[n].Connected)
{
string[] ss0 = textBox10.Text.Split(',');
string[] ss = textBox11.Text.Split(',');
if (ss0.Length != ss.Length)
{
 MessageBox.Show("地址数量与数据个数不一致。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
 return;
}
short[] dat = new short[MXObject.BUFFER_SIZE];
string saddr = "";
int ndx = 0;
if (radioButton1.Checked) //bit
{
 foreach(string s in ss)
 {
     try
     {
         dat[ndx] = (short)(Convert.ToInt16(s) == 0 ? 0 : 1);
         ndx++;
     }
     catch
     {
         MessageBox.Show("数据不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
 }
 saddr = textBox10.Text;
}
else
if (radioButton2.Checked)//short
{
 foreach (string s in ss)
 {
     try
     {
         dat[ndx] = Convert.ToInt16(s);
         ndx++;
     }
     catch
     {
         MessageBox.Show("数据不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
 }
 saddr = textBox10.Text;
}
else
if (radioButton3.Checked)//int32
{
 string[] _addr;
 foreach (string s in ss0)
 {
     _addr = PLC_Tag.SplitAddress(s);
     if (_addr[0].Length == 0 || _addr[1].Length == 0)
     {
         MessageBox.Show("地址格式不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
     saddr += _addr[0] + _addr[1] + ",";
     saddr += _addr[0] + (Convert.ToUInt16(_addr[1]) + 1).ToString() + ",";
 }
 short[] b;
 foreach (string s in ss)
 {
     try
     {
         b = MXObject.ToPLCWords(Convert.ToInt32(s));
         b.CopyTo(dat, ndx);
         ndx += 2;
     }
     catch
     {
         MessageBox.Show("数据不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
 }
}
else
if (radioButton4.Checked)//float
{
 string[] _addr;
 foreach (string s in ss0)
 {
     _addr = PLC_Tag.SplitAddress(s);
     if (_addr[0].Length == 0 || _addr[1].Length == 0)
     {
         MessageBox.Show("地址格式不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
     saddr += _addr[0] + _addr[1] + ",";
     saddr += _addr[0] + (Convert.ToUInt16(_addr[1]) + 1).ToString() + ",";
 }
 short[] b;
 foreach (string s in ss)
 {
     try
     {
         b = MXObject.ToPLCWords(Convert.ToSingle(s));
         b.CopyTo(dat, ndx);
         ndx += 2;
     }
     catch
     {
         MessageBox.Show("数据不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
 }
}
else
if (radioButton5.Checked)//string
{
 string[] _addr;
 UInt16 _tmp;
 foreach (string s in ss0)
 {
     _addr = PLC_Tag.SplitAddress(s);
     if (_addr[0].Length == 0 || _addr[1].Length == 0)
     {
         MessageBox.Show("地址格式不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
     _tmp = Convert.ToUInt16(_addr[1]);
     for (int i = 0; i < 16; i++)
     {
         saddr += _addr[0] + (_tmp + i).ToString() + ",";
     }
 }
 short[] b;
 foreach (string s in ss)
 {
     try
     {
         b = MXObject.ToPLCWords(s);
         b.CopyTo(dat, ndx);
         ndx += 16;
     }
     catch
     {
         MessageBox.Show("数据不正确。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
         return;
     }
 }
}
WRDataBlock db = new WRDataBlock();
db.address = MyThread.StrToByteArray(saddr.TrimEnd(','), MXObject.BUFFER_SIZE * 5);
db.dat_count = (short)ndx;
db.data = dat;
IntPtr buf = Marshal.AllocHGlobal(Marshal.SizeOf(db));
Marshal.StructureToPtr(db, buf, true);
Win32API.PostThreadMessage(threadList[n].ThreadId, WorkThread.WM_THREAD_WRITE, (int)buf, 0);
}
else
{
MessageBox.Show("plc还未连接，无法写入数据！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
}
*/
    }
}
