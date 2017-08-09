using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using INIFILE;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;




namespace SerialPortConnection
{


    public partial class Form1 : Form
    {
        SerialPort sp1 = new SerialPort();

        public Form1()
        {
            InitializeComponent();
        }

        //引用GDI用于设置字符间距
        [DllImport("gdi32.dll")]
        public static extern int SetTextCharacterExtra(IntPtr hdc, int nCharExtra);
        [DllImport("kernel32.dll")]
        static extern uint GetTickCount();



        public static bool gbOutputStrIsEmpt = true;//是否为第一次采用图像输出字符
        public static int gbImgWidth = 2000;//A4纸宽8.3inch，按照180dpi计算
        public static int gbImgHeigth = 2106;//A4纸宽11.7inch，按照180dpi计算
        public static Bitmap gbOutputPicture = new Bitmap(gbImgWidth, gbImgHeigth);//定义一张A4纸的大小的图片
        public static float gbXResolution;//X轴分辨率DPI
        public static float gbYResolution;//Y轴分辨率DPI
        
        public static string gbDecodeMethod = "gb18030";//解码方式
        public static string gbFontStyle = "宋体";//设定默认的西文字体
        public static float gbFontSize = 11.3F;//字体大小
        public static float gbNorSize = 11.3F;
        public static float gbDblSize = 19.5F;
        public static float gbNarSize = 11.3F;
        public static Font gbOldFntFont;// 定义旧字体
        public static Font gbNewFntFont;// 定义新字体           
        public static string gbReportSting;//最终报告输出字符串
        public static bool gbReportTxtOrBmp = true;//报告能否用TXT输出
        public static int intTsFrame = 0;
        //public static int intTsTotalData = 0;
        public static int flag = 0x0000;//存储标志符
        public static int m1 = 0;//判断串口缓存是否还有变化的中间值
        public static Byte[] receivedData = new Byte[1];        //创建接收字节数组
        public static int m = 0;//定义图像输出模式ESC * m nL nH中的m参数；m=0时为8点单密度图像，m=0x27时为24 点 3 倍密度图像
        public static int nL = 0;//定义图像输出模式ESC * m nL nH中的nL参数
        public static int nH = 0;//定义图像输出模式ESC * m nL nH中的nH参数
        public static int bytePerClumn = 0;//定义图像输出模式时每列由多少个字节构成
        public static int nByteOfBitmap = 0;//定义图像共有多少个字节
        public static float gbXPos = 0;//存储打印头位置
        public static float gbYPos = 0;
        public static float gbDelatX = 0;//X轴位移增量
        public static float gbDeltaY = 0;//Y轴位移增量
        public static bool gbDoubleCharSpace = false;//定义是否倍宽输出
        public static int gbCharSpace = 0;//定义字符间距
        public static List<Byte> recTxtData = new List<Byte>();  //定义转换后的报文数组
        //public static Byte[] recBmpData = new Byte[1];  //定义转换后的位图数组 
        System.Timers.Timer taskTimer = new System.Timers.Timer(500);//定义一个500ms的定时器
        public static List<int> maxXpos = new List<int>();
        public static List<float> xMoveValue = new List<float>();//记录X轴相对位移的数据
        public static int xMoveValueCount = 0;//记录收到了多少条位移的指令
        //public static bool txtEror = false;//设置在TXT显示状态下检测到图片输出时的标志
        public static bool autoResetDoubleSpace = false; //遇到0x1C 0E自动复位的倍宽模式时的标志

        //加载
        private void Form1_Load(object sender, EventArgs e)
        {
            INIFILE.Profile.LoadProfile();//加载所有
            gbNewFntFont = new Font(gbFontStyle, gbFontSize);//初始化字体
            gbOldFntFont = gbNewFntFont;//同步两种字体
            gbDeltaY = gbNewFntFont.Height + 0.5F;//初始化Y轴的递增量
                                                  // xMoveValue.Add(0.0F);//先占用一个list，并赋值为0；
                                                  //对原始的输出图片进行底色填充处理
            //gbOutputPicture.SetResolution(gbXResolution, gbYResolution);//set the image resolution
            using (Graphics g = Graphics.FromImage(gbOutputPicture))
            {
                g.FillRectangle(Brushes.White, 0, 0, gbImgWidth, gbImgHeigth);

            }
            taskTimer.Elapsed += new System.Timers.ElapsedEventHandler(timerReadSCom);//定义定时执行的任务
            taskTimer.AutoReset = true;//不断执行
            taskTimer.Enabled = false;//先不启用定时
            //输出控件预置
            tabSwitch.Controls.Clear();
            tabSwitch.Controls.Add(tabPage1);
            tabSwitch.Visible = true;

            // 预置波特率
            switch (Profile.G_BAUDRATE)
            {
                case "300":
                    cbBaudRate.SelectedIndex = 0;
                    break;
                case "600":
                    cbBaudRate.SelectedIndex = 1;
                    break;
                case "1200":
                    cbBaudRate.SelectedIndex = 2;
                    break;
                case "2400":
                    cbBaudRate.SelectedIndex = 3;
                    break;
                case "4800":
                    cbBaudRate.SelectedIndex = 4;
                    break;
                case "9600":
                    cbBaudRate.SelectedIndex = 5;
                    break;
                case "19200":
                    cbBaudRate.SelectedIndex = 6;
                    break;
                case "38400":
                    cbBaudRate.SelectedIndex = 7;
                    break;
                case "115200":
                    cbBaudRate.SelectedIndex = 8;
                    break;
                default:
                    {
                        MessageBox.Show("波特率预置参数错误。");
                        return;
                    }
            }

            //预置波特率
            switch (Profile.G_DATABITS)
            {
                case "5":
                    cbDataBits.SelectedIndex = 0;
                    break;
                case "6":
                    cbDataBits.SelectedIndex = 1;
                    break;
                case "7":
                    cbDataBits.SelectedIndex = 2;
                    break;
                case "8":
                    cbDataBits.SelectedIndex = 3;
                    break;
                default:
                    {
                        MessageBox.Show("数据位预置参数错误。");
                        return;
                    }

            }
            //预置停止位
            switch (Profile.G_STOP)
            {
                case "1":
                    cbStop.SelectedIndex = 0;
                    break;
                case "1.5":
                    cbStop.SelectedIndex = 1;
                    break;
                case "2":
                    cbStop.SelectedIndex = 2;
                    break;
                default:
                    {
                        MessageBox.Show("停止位预置参数错误。");
                        return;
                    }
            }

            //预置校验位
            switch (Profile.G_PARITY)
            {
                case "NONE":
                    cbParity.SelectedIndex = 0;
                    break;
                case "ODD":
                    cbParity.SelectedIndex = 1;
                    break;
                case "EVEN":
                    cbParity.SelectedIndex = 2;
                    break;
                default:
                    {
                        MessageBox.Show("校验位预置参数错误。");
                        return;
                    }
            }

            //检查是否含有串口
            string[] str = SerialPort.GetPortNames();
            if (str == null)
            {
                MessageBox.Show("本机没有串口！", "Error");
                return;
            }

            //添加串口项目
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {//获取有多少个COM口
                //System.Diagnostics.Debug.WriteLine(s);
                cbSerial.Items.Add(s);
            }

            //串口设置默认选择项
            cbSerial.SelectedIndex = 0;         //note：获得COM9口，但别忘修改
                                                //cbBaudRate.SelectedIndex = 5;
                                                // cbDataBits.SelectedIndex = 3;
                                                // cbStop.SelectedIndex = 0;
                                                //  cbParity.SelectedIndex = 0;
            sp1.BaudRate = 9600;

            Control.CheckForIllegalCrossThreadCalls = false;    //这个类中我们不检查跨线程的调用是否合法(因为.net 2.0以后加强了安全机制,，不允许在winform中直接跨线程访问控件的属性)
            //sp1.DataReceived += new SerialDataReceivedEventHandler(sp1_DataReceived);
            //sp1.ReceivedBytesThreshold = 128;//事件发生前内部输入缓冲区的字节数，每当缓冲区的字节达到此设定的值，就会触发对象的数据接收事件
            //准备就绪              
            sp1.DtrEnable = true;
            sp1.RtsEnable = true;
            //设置数据读取超时为0.5秒
            sp1.ReadTimeout = 300;
            sp1.ReadBufferSize = 1024 * 1024 * 30;//设置串口缓存大小为30M
            sp1.Close();
        }

        //定时读串口
        void timerReadSCom(object sender, ElapsedEventArgs e)
        {
            if (sp1.IsOpen)     //此处可能没有必要判断是否打开串口，但为了严谨性，我还是加上了
            {
                try
                {
                    if (gbReportTxtOrBmp == true)
                    {
                        txtHandle();
                    }
                    else if (gbReportTxtOrBmp == false)
                    {
                        bmpHandle();
                    }

                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message, "出错提示");
                    //txtReceive.Text = "";
                }
            }
            else
            {
                //MessageBox.Show("请打开某个串口", "错误提示");
                tsTips.Text = "状态: 当前串口不存在，请打开某个串口！";
            }
        }

        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2")+" ";//用空格分隔字节数据
                }
            }
            return returnStr;
        }

        public void SaveData( byte[] data)
        {
            string strTobeSave = byteToHexStr(data);
            string filePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "aa.txt";//获取程序运行的目录
            StreamWriter FileWriter = new StreamWriter(filePath, false); //写文件
            FileWriter.Write(strTobeSave);//将字符串写入
            FileWriter.Close(); //关闭StreamWriter对象
        }


        private void bmpHandle()
        {
            try
            {
                if (sp1.BytesToRead > 0)//串口缓存有数据
                {

                    if (sp1.BytesToRead > m1)
                    {
                        m1 = sp1.BytesToRead;
                        //intTsTotalData += m1;
                        tsFrame.Text = "当前缓存数据: " + Convert.ToString(m1 / 1024) + "k" + Convert.ToString(m1 % 1024) + "Byte|";
                        //tsTotalData.Text = "总接收数据: " + Convert.ToString(intTsTotalData/1024)+" kB|";

                        // tsTotalData.Text = "图片大小: "+ maxXpos.Max().ToString()+"x"+ Convert.ToString((int)gbYPos) + " |";
                        tsTips.Text = "状态: 正在接收数据,请稍等...";
                    }//如果串口缓存数值比上次有增长，则用当前值赋给m1
                    else
                    {
                        //taskTimer.Enabled = false;
                        receivedData = new byte[sp1.BytesToRead];        //创建接收字节临时数组
                        sp1.Read(receivedData, 0, receivedData.Length);
                        SaveData(receivedData);//save the receive data
                        
                        tsTips.Text = "状态: 数据接收完毕，正在计算处理。";
                    }
                }
                else if (sp1.BytesToRead <= 0 && m1 > 0)//串口缓存数据已读完
                {
                    taskTimer.Enabled = false;
                    // Byte[] recTxtData = new Byte[1024*4];//定义转换后的报文数组,大小为4k；
                    //数据预处理，获取DPI值
                    int recDataCount = 0;//处理前数组维度计数
                    while(recDataCount <receivedData.Length)
                    {
                        switch(flag)
                        {
                            case 0x00:
                                switch(receivedData[recDataCount])
                                {
                                    case 0x1B:
                                        flag = 0x1B;
                                        break;
                                    default:
                                        flag = 0x00;
                                        break;
                                }
                                break;
                            case 0x1B:
                                switch (receivedData[recDataCount])
                                {
                                    case 0x2A:
                                        flag = 0x1B2A;
                                        break;
                                    default:
                                        flag = 0x00;
                                        break;
                                }
                                break;
                            case 0x1B2A:
                                switch (receivedData[recDataCount])
                                {
                                    case 0:
                                        bytePerClumn = 1;
                                        gbXResolution = 60;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 1:
                                        bytePerClumn = 1;
                                        gbXResolution = 120;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 2:
                                        bytePerClumn = 1;
                                        gbXResolution = 120;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 3:
                                        bytePerClumn = 1;
                                        gbXResolution = 240;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 4:
                                        bytePerClumn = 1;
                                        gbXResolution = 80;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 32:
                                        bytePerClumn = 3;
                                        gbXResolution = 60;//水平分辨率为60dpi
                                        gbYResolution = 180;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 33:
                                        bytePerClumn = 3;
                                        gbXResolution = 120;//水平分辨率为60dpi
                                        gbYResolution = 180;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 38:
                                        bytePerClumn = 3;
                                        gbXResolution = 90;//水平分辨率为90dpi
                                        gbYResolution = 180;//垂直分辨率为180dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                    case 39:
                                        bytePerClumn = 3;
                                        gbXResolution = 180;//水平分辨率为60dpi
                                        gbYResolution = 180;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;                                                                     
                                        break;
                                    default:
                                        bytePerClumn = 6;
                                        gbXResolution = 60;//水平分辨率为60dpi
                                        gbYResolution = 60;//垂直分辨率为60dpi
                                        gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
                                        flag = 0x00;
                                        break;
                                }
                                gbDeltaY = bytePerClumn * 8;//在不同的参数值下，每次遇到0x0A命令时，Y轴的递增量
                                recDataCount = receivedData.Length;//直接置计数为数据接收总长，以跳出循环
                                break;
                        }
                        recDataCount++;
                    }


                    //将命令与内容分离
                    Byte[] recBmpData = new Byte[1]; //定义转换后的位图二维数组
                    List<Byte[]> bmpList = new List<byte[]>();//定义图片list
                    List<Byte[]> strList = new List<byte[]>();//定义字符串list
                    
                    int jTxt = 0;//存储处理后的TXT数组维度计数
                    int jBitmap = 0;//存储处理后的bitmap数组维度计数
                                    //int nBitmapCmd = 0;//存储共收到了多少次位图指令
                    int numBitmap = 0;//检测有多少次位图命令
                    bool strMode = false;//定义是否位文本输出
                    bool isCmd = false;//定义是否位命令
                    bool setAbsPos = false;//是否存在设定X的绝对位置
                    int xposnl = 0;//指定打印绝对位置的nl变量

                    float xposTemp = 0;

                    recDataCount = 0;//reset the counter
                    while (recDataCount < receivedData.Length)
                    {
                        switch (flag)
                        {
                            case 0x00:
                                switch (receivedData[recDataCount])
                                {
                                    case 0x07://BEL，依据命令单元而舍弃。
                                        isCmd = true;
                                        break;
                                    case 0x08://BS，倒回一个文字位。                                       
                                        break;
                                    case 0x09://HT，向水平跳格位置移动。
                                        break;
                                    case 0x0A://换行
                                        gbYPos = gbYPos + gbDeltaY;
                                        isCmd = true;
                                        if (autoResetDoubleSpace == true) { gbCharSpace = 0; gbDoubleCharSpace = true; autoResetDoubleSpace = false; }
                                        break;
                                    case 0x0B://VT，到垂直跳格位置为止换行；
                                        isCmd = true;
                                        break;
                                    case 0x0C://FF，换页；
                                        gbXPos = 0;
                                        gbYPos = gbYPos + gbDeltaY;
                                        isCmd = true;
                                        break;
                                    case 0x0d://CR，打印复位
                                        maxXpos.Add((int)(gbXPos + 0.5));
                                        gbXPos = 0;
                                        setAbsPos = false;
                                        xMoveValue = new List<float>();
                                        isCmd = true;
                                        if (autoResetDoubleSpace == true) { gbCharSpace = 0; gbDoubleCharSpace = true; autoResetDoubleSpace = false;  }
                                        break;
                                    case 0x0E://设置倍宽打印
                                              //gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中
                                              //gbFontSize = gbDblSize;//                                                                   
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                              // gbDoubleCharSpace = true;//倍宽输出标志位置true
                                        gbCharSpace = 8;
                                        isCmd = true;
                                        break;
                                    case 0x0F://设定ANK缩小打印
                                        gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中 
                                        gbFontSize = gbNarSize;
                                        gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        isCmd = true;
                                        break;
                                    case 0x11://DC1，将接收数据设为有效
                                        isCmd = true;
                                        break;
                                    case 0x12://解除ANK缩小打印
                                        gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中
                                        gbFontSize = gbNorSize;
                                        gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        isCmd = true;
                                        break;
                                    case 0x13://DC3，将 DC1 以外的接收数据设为无效
                                        isCmd = true;
                                        break;
                                    case 0x14://DC4，取消倍宽打印模式
                                              //gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中  
                                              //gbFontSize = gbNorSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体 
                                              // gbDoubleCharSpace = false;//倍宽输出标志位置true         
                                        gbCharSpace = 0;
                                        isCmd = true;
                                        break;
                                    case 0x18://清楚打印缓冲器
                                        isCmd = true;
                                        break;
                                    case 0x1B://ESC命令，                                        
                                        flag = 0x1B;
                                        //isCmd = true;
                                        break;
                                    case 0x1C://FS命令 
                                        flag = 0x1C;
                                        //isCmd = true;
                                        break;
                                    case 0x7F://DEL，清除前一个文字
                                        isCmd = true;
                                        break;
                                    case 0x00://对空字符串进行处理
                                        recTxtData.Add(0x20);// 用空格填充
                                        gbOutputStrIsEmpt = false;
                                        strMode = true;
                                        jTxt++;
                                        isCmd = false;
                                        break;
                                    default:
                                        recTxtData.Add(receivedData[recDataCount]); // 将非控制命令数据赋值给处理后的数组
                                        gbOutputStrIsEmpt = false;
                                        strMode = true;
                                        jTxt++;
                                        isCmd = false;
                                        break;
                                }
                                break;
                            case 0x1B://ESC命令
                                switch (receivedData[recDataCount])
                                {
                                    case 0x23://解除 MSB 的控制。
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x24://指定绝对打印位置（1/60 英寸）；
                                        flag = 0x1B2401;

                                        break;
                                    case 0x30://设定 1/8 英寸行距
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x32://设定 1/6 英寸行距
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x33://设定 n/180 英寸的行距
                                        flag = 0x1B33;
                                        break;
                                    case 0x41://设定 n/60 英寸的行距
                                        flag = 0x1B41;
                                        break;
                                    case 0x2B://设定 n/360 英寸行距,
                                        flag = 0x1B2B;
                                        break;
                                    case 0x34://设定斜体打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x35://取消斜体打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x36://解除移出侧控制代码。
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x37://设定移出侧控制代码
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x3C://将打印头返回左端
                                        gbXPos = 0;
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x3D://将接收数据的 MSB 设为 0。
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x3E://将接收数据的 MSB 设为1。
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x40://进行打印机的初始化
                                        flag = 0x00;//复位标识符
                                                    //清空所有数据
                                        isCmd = true;
                                        break;
                                    case 0x45://设定加黑打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x46://取消加黑打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x47://设定重打模式
                                        flag = 0x00;
                                        break;
                                    case 0x48://取消重打模式
                                        flag = 0x00;
                                        break;
                                    case 0x51://设定右侧边界
                                        flag = 0x1B51;//
                                        break;
                                    case 0x55://
                                        flag = 0x1B55; //n = 00 或 n = 30, 解除单向打印模式; n = 01 或 n = 31, 设定单向打印模式;
                                        break;
                                    case 0x5C://指定右侧（左侧）的相对打印位置（指定字点单位）

                                        flag = 0x1B5C01;
                                        // outputNow = true;
                                        break;
                                    case 0x6C://设定左侧边界
                                        flag = 0x1B6C;
                                        break;
                                    case 0x2A://图像输出模式,ESC * m nL nH 传送 24 点 3 倍密度图像 ESC * 39 n1 n2 
                                        flag = 0x1B2A01;
                                        strMode = false;
                                        break;
                                    case 0x0E://设置自动复位的倍宽打印模式
                                              //gbFontSize = gbDblSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        gbCharSpace = 8;
                                        gbDoubleCharSpace = true;//倍宽输出标志位置true
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        autoResetDoubleSpace = true;
                                        break;
                                    default:
                                        flag = 0x00;
                                        recTxtData.Add(receivedData[recDataCount]); // 将非控制命令数据赋值给处理后的数组
                                        gbOutputStrIsEmpt = false;
                                        strMode = true;
                                        jTxt++;
                                        isCmd = false;
                                        break;
                                }
                                break;
                            case 0x1C://FS命令
                                switch (receivedData[recDataCount])
                                {
                                    case 0x26://设定汉字模式
                                        gbDecodeMethod = "gb18030";//采用gb18030字符集解码
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x2E://解除汉字模式
                                        gbDecodeMethod = "ascii";//采用ASCII字符集解码
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x0F://设定半角文字模式
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x12://解除半角文字模式
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x6B://0 - 汉字字体选择为宋体,1 - 汉字字体选择为黑体。
                                        flag = 0x1C6B;
                                        break;
                                    case 0x78:
                                        flag = 0x1C78;//汉字高速打印模式
                                        break;
                                    case 0x0E://设置倍宽打印模式
                                              //gbFontSize = gbDblSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        gbCharSpace = 8;
                                        gbDoubleCharSpace = true;//倍宽输出标志位置true
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x14://解除倍宽打印模式
                                              //gbFontSize = gbNorSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        gbCharSpace = 0;
                                        gbDoubleCharSpace = false;//倍宽输出标志位置true
                                        flag = 0x00;//复位标识符
                                        isCmd = true;
                                        break;
                                    case 0x53://设置全角文字间距:第一个参数为左空白,第二个参数为右空白
                                        flag = 0x1C5301;//先获取第一个参数
                                        break;
                                    default:
                                        flag = 0x00;
                                        recTxtData.Add(receivedData[recDataCount]); // 将非控制命令数据赋值给处理后的数组
                                        gbOutputStrIsEmpt = false;
                                        strMode = true;
                                        jTxt++;
                                        isCmd = false;
                                        break;
                                }
                                break;
                            case 0x1B2401://指定绝对打印位置，获取第1个参数
                                xposnl = receivedData[recDataCount];
                                flag = 0x1B2402;//跳转至第2个参数
                                //isCmd = true;
                                break;
                            case 0x1B2402://指定绝对打印位置，获取第2个参数
                                gbXPos =( receivedData[recDataCount] * 256 + xposnl )* gbOutputPicture.HorizontalResolution/180;//默认移动单位是1/120英寸
                                Console.WriteLine("x轴" + gbXPos+ "inch");
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B33://设定 n/180 英寸的行距，获取n参数
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B41://设定 n/60 英寸的行距，获取n参数
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B2B://设定 n/360 英寸行距,，获取n参数
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B55://单向打印模式,n = 00 或 n = 30,解除单向打印模式;n = 01 或 n = 31,设定单向打印模式;
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B5C01://指定相对打印位置,获取第1个参数，
                                nL = receivedData[recDataCount];
                                flag = 0x1B5C02;//跳转至第2个参数
                                                //isCmd = true;
                                break;
                            case 0x1B5C02://指定相对打印位置,获取第2个参数，
                                nH = receivedData[recDataCount];
                                xposTemp = (nH * 256 + nL )* gbOutputPicture.HorizontalResolution / 120;//默认移动单位是1/60英寸;
                                xMoveValue.Add(xposTemp);
                               // xMoveValue.Add(xposTemp * 1.15F);//存入list中                                    
                                flag = 0x00;//复位标识符
                                setAbsPos = true;
                                isCmd = true;
                                break;
                            case 0x1B51://设定右侧边界
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B6C://设定左侧边界
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1C6B://汉字选择，n = 00，汉字字体选择为宋体；n = 01，汉字字体选择为黑体；
                                if (receivedData[recDataCount] == 0x00) { gbFontStyle = "宋体"; }
                                if (receivedData[recDataCount] == 0x01) { gbFontStyle = "黑体"; }
                                gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体                                                                                  
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1C78://汉字高速打印
                                flag = 0x00;
                                break;
                            case 0x1C5301://获取全角文字间距的左侧空白量
                                flag = 0x1C5302;//跳转至第2个参数
                                break;
                            case 0x1C5302://获取全角文字间距的右侧空白量
                                flag = 0x00;//复位标识符
                                isCmd = true;
                                break;
                            case 0x1B2A01://获取图像输出命令的第1个参数，m
                                flag = 0x1B2A02;
                                strMode = false;                           
                                break;
                            case 0x1B2A02://获取图像输出命令的第2个参数，nL
                                flag = 0x1B2A03;
                                nL = receivedData[recDataCount];
                                strMode = false;
                                break;
                            case 0x1B2A03://获取图像输出命令的第3个参数，nH
                                nH = receivedData[recDataCount];
                                nByteOfBitmap = (nH * 256 + nL) * bytePerClumn;
                                numBitmap++;
                                flag = 0x1111; //位图处理
                                tsTips.Text = "状态: 检测到波形打印命令，正在生成图片(" + Convert.ToString(numBitmap) + ")。";
                                recBmpData = new Byte[nByteOfBitmap];//获取图像数组的大小？？？？？
                                strMode = false;
                                break;

                            case 0x1111://位图处理
                                isCmd = false;
                                recBmpData[jBitmap] = receivedData[recDataCount];
                                jBitmap++;
                                if (jBitmap == nByteOfBitmap - 1 && m1 != 0)//已经到最后一个数
                                {
                                    recBmpData[jBitmap] = receivedData[recDataCount + 1];
                                    recDataCount++;
                                    flag = 0x00;
                                    jBitmap = 0;
                                    //nBitmapCmd++;
                                    bmpList.Add(recBmpData);
                                    //displayReportPic(gbXPos, gbYPos, bmpList[0], gbFntFont, false);
                                    //updatePictureDisplay(bmpList[0],false,gbNewFntFont,false );//更新输出
                                    bitmapDisp((int)gbXPos, (int)gbYPos, bmpList[0]);//输出图像
                                    bmpList.Clear();
                                }
                                tsTotalData.Text = "图片大小: " + maxXpos.Max().ToString() + "x" + Convert.ToString((int)gbYPos) + " |";
                                break;
                            default:
                                break;
                        }
                        if (isCmd == true && strMode == true && gbOutputStrIsEmpt == false)//在下个命令时触发输出，控制命令区，并且字符串曾被触发，此种方法的弊端在于字符串后发必须有命令控制字符
                        {
                            //对X轴绝对值的处理
                            if (xMoveValue.Count == 1 && setAbsPos == true)
                            { gbXPos += xMoveValue[xMoveValue.Count - 1]; setAbsPos = false; }
                            else if (xMoveValue.Count > 1 && setAbsPos == true)
                            { gbXPos += xMoveValue[xMoveValue.Count - 2]; setAbsPos = false; }
                            else if (xMoveValue.Count > 1 && setAbsPos == false)
                            { gbXPos += xMoveValue[xMoveValue.Count - 1]; xMoveValue = new List<float>(); }


                            switch (receivedData[recDataCount])
                            {
                                case 0x0A:
                                    stringDisp((int)gbXPos, (int)(gbYPos - gbDeltaY), recTxtData, gbCharSpace, gbDecodeMethod, gbNewFntFont);
                                    //updatePictureDisplay(recTxtData, true, gbNewFntFont,true );//回滚更新输出
                                    break;
                                case 0x1E://设置倍宽打印
                                          //updatePictureDisplay(recTxtData, false, gbOldFntFont, true);//旧字体输出
                                    stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbOldFntFont);
                                    break;
                                case 0x0F://设定ANK缩小打印
                                          // updatePictureDisplay(recTxtData, false, gbOldFntFont, true);//旧字体输出
                                    stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbOldFntFont);
                                    break;
                                case 0x12://解除ANK缩小打印
                                          // updatePictureDisplay(recTxtData, false, gbOldFntFont, true);//旧字体输出
                                    if (receivedData[recDataCount - 1] == 0x1C)//如果指令是0x1C12
                                    {
                                        stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbNewFntFont);
                                    }
                                    else
                                    {
                                        stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbOldFntFont);
                                    }

                                    break;
                                case 0x14://DC4，取消倍宽打印模式
                                          //updatePictureDisplay(recTxtData, false, gbOldFntFont, true);//旧字体输出
                                    stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbNewFntFont);
                                    break;
                                default:
                                    //updatePictureDisplay(recTxtData, false, gbNewFntFont, true);//回滚更新输出
                                    stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbNewFntFont);
                                    break;
                            }
                            //recTxtData = new byte[1024 * 4];//清空数组
                            recTxtData = new List<byte>();
                            jTxt = 0;
                            gbOutputStrIsEmpt = true;
                            strMode = false;
                            //   outputNow = false;
                            flag = 0x00;
                        }
                        recDataCount++;
                        if (recDataCount == receivedData.Length)
                        {
                            if (jTxt != 0 && gbOutputStrIsEmpt == true && strMode == true)//存在字符串数组未输出
                            {
                                if (xMoveValue.Count == 1 && setAbsPos == true)
                                { gbXPos += xMoveValue[xMoveValue.Count - 1]; setAbsPos = false; }
                                else if (xMoveValue.Count > 1 && setAbsPos == true)
                                { gbXPos += xMoveValue[xMoveValue.Count - 2]; setAbsPos = false; }
                                else if (xMoveValue.Count > 1 && setAbsPos == false)
                                { gbXPos += xMoveValue[xMoveValue.Count - 1]; xMoveValue = new List<float>(); }
                                // updatePictureDisplay(recTxtData,false,gbNewFntFont, true);//更新输出
                                stringDisp((int)gbXPos, (int)gbYPos, recTxtData, gbCharSpace, gbDecodeMethod, gbNewFntFont);
                                //recTxtData = new byte[1024 * 4];//清空数组
                                recTxtData = new List<byte>();
                                jTxt = 0;
                                gbOutputStrIsEmpt = true;
                                strMode = false;
                                flag = 0x00;
                            }
                            m1 = 0;//复位缓存是否还有数据的标志                        
                        }
                    }
                    cutPicture();
                    reportPictureBox.Image = gbOutputPicture;
                    //Delay(5);
                    tsTips.Text = "状态: 图像处理任务完成。";
                }
                taskTimer.Enabled = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "出错提示");
                //txtReceive.Text = "";
            }
        }

        private void txtHandle()
        {
            try
            {
                if (sp1.BytesToRead > 0)//串口缓存有数据
                {

                    if (sp1.BytesToRead > m1)
                    {
                        m1 = sp1.BytesToRead;
                        //intTsTotalData += m1;
                        tsFrame.Text = "当前缓存数据: " + Convert.ToString(m1) + " Byte|";
                        //tsTotalData.Text = "总接收数据: " + Convert.ToString(intTsTotalData/1024) + " kB|";
                        tsTotalData.Text = "**** |";
                        tsTips.Text = "状态: 正在接收数据,请稍等...";
                    }//如果串口缓存数值比上次有增长，则用当前值赋给m1
                    else
                    {
                        //taskTimer.Enabled = false;
                        receivedData = new byte[sp1.BytesToRead];        //创建接收字节临时数组

                        sp1.Read(receivedData, 0, receivedData.Length);
                        tsTips.Text = "状态:数据接收完毕，正在计算处理。";
                    }
                }
                else if (sp1.BytesToRead <= 0 && m1 > 0)//串口缓存数据已读完
                {
                    taskTimer.Enabled = false;

                    Byte[] recTxt = new Byte[receivedData.Length];
                    int i = 0;//处理前数组维度计数
                    int jTxt = 0;//存储处理后的TXT数组维度计数
                    bool txtEror = false;//定义是否在txt输出界面中存在输出图片的命令



                    while (i < receivedData.Length)
                    {
                        switch (flag)
                        {
                            case 0x00:
                                switch (receivedData[i])
                                {
                                    case 0x07://BEL，依据命令单元而舍弃。                                   
                                        break;
                                    case 0x08://BS，倒回一个文字位。                                       
                                        break;
                                    case 0x09://HT，向水平跳格位置移动。
                                        break;
                                    case 0x0A://换行
                                        gbYPos = gbYPos + gbDeltaY;
                                        break;
                                    case 0x0B://VT，到垂直跳格位置为止换行；
                                        break;
                                    case 0x0C://FF，换页；
                                        break;
                                    //case 0x0d://CR，打印复位
                                    //    break;
                                    case 0x0E://设置倍宽打印                                
                                              //gbCharSpace = 8;
                                        break;
                                    case 0x0F://设定ANK缩小打印
                                              //gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中 
                                              //gbFontSize = gbNarSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        break;
                                    case 0x11://DC1，将接收数据设为有效
                                        break;
                                    case 0x12://解除ANK缩小打印
                                              //gbOldFntFont = gbNewFntFont;//把之前的字体存储到旧字体中
                                              //gbFontSize = gbNorSize;
                                              //gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体
                                        break;
                                    case 0x13://DC3，将 DC1 以外的接收数据设为无效
                                        break;
                                    case 0x14://DC4，取消倍宽打印模式
                                              //gbCharSpace = 0;
                                        break;
                                    case 0x18://清楚打印缓冲器
                                        break;
                                    case 0x1B://ESC命令，                                        
                                        flag = 0x1B;
                                        break;
                                    case 0x1C://FS命令 
                                        flag = 0x1C;
                                        break;
                                    case 0x7F://DEL，清除前一个文字
                                        break;
                                    case 0x00://对空字符串进行处理
                                        recTxt[jTxt] = 0x20; // 用空格填充                                                                  
                                        jTxt++;
                                        break;
                                    default:
                                        recTxt[jTxt] = receivedData[i]; // 将非控制命令数据赋值给处理后的数组                                    
                                        jTxt++;
                                        break;
                                }
                                break;
                            case 0x1B://ESC命令
                                switch (receivedData[i])
                                {
                                    case 0x23://解除 MSB 的控制。
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x24://指定绝对打印位置（1/60 英寸）；
                                        flag = 0x1B2401;
                                        break;
                                    case 0x30://设定 1/8 英寸行距
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x32://设定 1/6 英寸行距
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x33://设定 n/180 英寸的行距
                                        flag = 0x1B33;
                                        break;
                                    case 0x41://设定 n/60 英寸的行距
                                        flag = 0x1B41;
                                        break;
                                    case 0x2B://设定 n/360 英寸行距,
                                        flag = 0x1B2B;
                                        break;
                                    case 0x34://设定斜体打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x35://取消斜体打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x36://解除移出侧控制代码。
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x37://设定移出侧控制代码
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x3C://将打印头返回左端
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x3D://将接收数据的 MSB 设为 0。
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x3E://将接收数据的 MSB 设为1。
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x40://进行打印机的初始化
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x45://设定加黑打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x46://取消加黑打印模式
                                        flag = 0x00;
                                        break;
                                    case 0x47://设定重打模式
                                        flag = 0x00;
                                        break;
                                    case 0x48://取消重打模式
                                        flag = 0x00;
                                        break;
                                    case 0x51://设定右侧边界
                                        flag = 0x1B51;//
                                        break;
                                    case 0x55://
                                        flag = 0x1B55; //n = 00 或 n = 30, 解除单向打印模式; n = 01 或 n = 31, 设定单向打印模式;
                                        break;
                                    case 0x5C://指定右侧（左侧）的相对打印位置（指定字点单位）
                                        flag = 0x1B5C01;
                                        break;
                                    case 0x6C://设定左侧边界
                                        flag = 0x1B6C;
                                        break;
                                    case 0x2A://图像输出模式,ESC * m nL nH 传送 24 点 3 倍密度图像 ESC * 39 n1 n2 
                                        flag = 0x1B2A01;
                                        break;
                                    case 0x0E://设置倍宽打印模式
                                        flag = 0x00;//复位标识符
                                        break;
                                    default:
                                        flag = 0x00;
                                        recTxt[jTxt] = receivedData[i]; // 将非控制命令数据赋值给处理后的数组                                    
                                        jTxt++;
                                        break;
                                }
                                break;
                            case 0x1C://FS命令
                                switch (receivedData[i])
                                {
                                    case 0x26://设定汉字模式
                                              //gbDecodeMethod = "gb18030";//采用gb18030字符集解码
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x2E://解除汉字模式
                                              //gbDecodeMethod = "ascii";//采用ASCII字符集解码
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x0F://设定半角文字模式
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x12://解除半角文字模式
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x6B://0 - 汉字字体选择为宋体,1 - 汉字字体选择为黑体。
                                        flag = 0x1C6B;
                                        break;
                                    case 0x78:
                                        flag = 0x1C78;//汉字高速打印模式
                                        break;
                                    case 0x0E://设置倍宽打印模式
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x14://解除倍宽打印模式位置true
                                        flag = 0x00;//复位标识符
                                        break;
                                    case 0x53://设置全角文字间距:第一个参数为左空白,第二个参数为右空白
                                        flag = 0x1C5301;//先获取第一个参数
                                        break;
                                    default:
                                        flag = 0x00;
                                        recTxt[jTxt] = receivedData[i]; // 将非控制命令数据赋值给处理后的数组                                    
                                        jTxt++;
                                        break;
                                }
                                break;
                            case 0x1B2401://指定绝对打印位置，获取第1个参数
                                          //  int nlxpos   = receivedData[i];
                                flag = 0x1B2402;//跳转至第2个参数
                                break;
                            case 0x1B2402://指定绝对打印位置，获取第2个参数
                                          // int nh = receivedData[i];
                                          // gbXPos = nh * 256 + nl;
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B33://设定 n/180 英寸的行距，获取n参数
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B41://设定 n/60 英寸的行距，获取n参数
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B2B://设定 n/360 英寸行距,，获取n参数
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B55://单向打印模式,n = 00 或 n = 30,解除单向打印模式;n = 01 或 n = 31,设定单向打印模式;
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B5C01://指定相对打印位置,获取第1个参数，
                                          //nL = receivedData[i];
                                flag = 0x1B5C02;//跳转至第2个参数
                                                //isCmd = true;
                                break;
                            case 0x1B5C02://指定相对打印位置,获取第2个参数，
                                          //nH = receivedData[i];
                                          //xposTemp = nH * 256 + nL;
                                          //xMoveValue.Add(xposTemp * 1.15F);//存入list中                                    
                                flag = 0x00;//复位标识符
                                            //setAbsPos = true;
                                break;
                            case 0x1B51://设定右侧边界
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B6C://设定左侧边界
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1C6B://汉字选择，n = 00，汉字字体选择为宋体；n = 01，汉字字体选择为黑体；
                                if (receivedData[i] == 0x00) { gbFontStyle = "宋体"; }
                                if (receivedData[i] == 0x01) { gbFontStyle = "黑体"; }
                                gbNewFntFont = new Font(gbFontStyle, gbFontSize);//根据命令设定的参数构造字体                                                                                  
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1C78://汉字高速打印
                                flag = 0x00;
                                break;
                            case 0x1C5301://获取全角文字间距的左侧空白量
                                flag = 0x1C5302;//跳转至第2个参数
                                break;
                            case 0x1C5302://获取全角文字间距的右侧空白量
                                flag = 0x00;//复位标识符
                                break;
                            case 0x1B2A01://获取图像输出命令的第1个参数，m
                                flag = 0x1B2A02;
                                break;
                            case 0x1B2A02://获取图像输出命令的第2个参数，nL
                                flag = 0x1B2A03;
                                break;
                            case 0x1B2A03://获取图像输出命令的第3个参数，nH                            
                                          //填写出错函数
                                if (gbReportTxtOrBmp == true)//txt模式输出
                                {
                                    flag = 0x00;
                                    txtEror = true;
                                    i = receivedData.Length - 1;//跳过后面的参数
                                    tsTips.Text = "状态: 检测到波形打印命令，文字模式下无法显示，请切换至图片模式！";
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            default:
                                break;
                        }

                        i++;

                    }
                    if (txtEror == false)
                    {
                        System.Text.Encoding chs = System.Text.Encoding.GetEncoding("gb18030");//采用GB18030编码
                        String decodedString = chs.GetString(recTxt);
                        txtReceive.Text += decodedString;
                        txtReceive.Focus();
                        txtReceive.Select(txtReceive.TextLength, 0);
                        txtReceive.ScrollToCaret();
                        m1 = 0;//复位缓存是否还有数据的标志      
                        tsTips.Text = "状态:文字处理任务完成。";
                    }
                    else if (txtEror == true)
                    {
                        tsTips.Text = "状态: 检测到波形打印命令，文字模式下无法显示，请切换至图片模式！";
                    }
                }
                taskTimer.Enabled = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "出错提示");
                //txtReceive.Text = "";
            }
        }


        //x,y为字符左上角的位置，content为未经解码的数组；decode为解码方式，fonstyle为字体
        public float stringDisp(int x, int y, List<Byte> content, int CharSpace, string decode, Font fontStyle)
        {
            float stringWidth = 0;
            System.Text.Encoding chs = System.Text.Encoding.GetEncoding(decode);//采用GB18030编码
            if (y > gbOutputPicture.Height - gbDeltaY)
            {
                addBlackSpace();
            }
            Byte[] aaaa = new Byte[content.Count];
            for (int i = 0; i < content.Count; i++)
            { aaaa[i] = content[i]; }
            String decString = chs.GetString(aaaa);

            Image imageClone = (Image)gbOutputPicture.Clone();

            using (Graphics g = Graphics.FromImage(imageClone))
            {
                //SizeF stringSize = g.MeasureString(decString, fontStyle);
                SizeF stringSize = TextRenderer.MeasureText(decString, fontStyle);
                stringWidth = stringSize.Width;
                gbXPos += stringWidth;
                maxXpos.Add((int)(gbXPos + 0.5F));
                IntPtr hdc = g.GetHdc();
                SetTextCharacterExtra(hdc, CharSpace); // 设置字符间距
                g.ReleaseHdc(hdc);
                Point pos = new Point(x, y);
                //Console.WriteLine("字符串“" + decString + "”长度为" + stringWidth);
                //Console.WriteLine("“"+decString + "”起始位置为：X-" + x + "；Y-" + y);
                //Console.WriteLine("“" + decString + "”结束位置为：X-" + gbXPos + "；Y-" + y);
                //System.Threading.Thread.Sleep(5);
                TextRenderer.DrawText(g, decString, fontStyle, pos, Color.Black);
            }
            //Delay(10);
            gbOutputPicture = (Bitmap)imageClone.Clone();
            reportPictureBox.Image = gbOutputPicture;//输出界面
            return stringWidth;
        }


        //x,y为画图的左上角的位置，content为未经解码的数组；
        public void bitmapDisp(int x, int y, Byte[] content)
        {
            if (y > gbOutputPicture.Height - gbDeltaY)
            {
                addBlackSpace();
            }
            Image imageClone = (Image)gbOutputPicture.Clone();
            using (Graphics g = Graphics.FromImage(imageClone))
            {
                //输出波形
                Brush brush = Brushes.Black;
                int z;
                int idata = 0;
                for (int iRow = 0; iRow < nByteOfBitmap / bytePerClumn; iRow++)
                {

                    for (int xTemp = 1; xTemp < 4; xTemp++)
                    {
                        for (int yTemp = 0; yTemp < 8; yTemp++)
                        {
                            z = 1 << yTemp;
                            if ((content[idata] & z) != 0)
                            {
                                g.FillRectangle(brush, gbXPos, gbYPos + ((xTemp - 1) * 8) + (7 - yTemp), 1, 1);

                            }
                        }
                        idata++;
                        if (xTemp == 3) { gbXPos++; }
                    }
                }
                //Console.WriteLine("gbXPos=“" + gbXPos + "”");
                //maxXpos.Add((int)(gbXPos + 0.5));           
            }
            //Delay(10);
            gbOutputPicture = (Bitmap)imageClone.Clone();
            //cutPicture();
            reportPictureBox.Image = gbOutputPicture;//输出界面
        }

        public void addBlackSpace()
        {
            Image imageClone = (Image)gbOutputPicture.Clone();
            int H = imageClone.Height;
            int W = imageClone.Width;
            int A4H = 2106;//定义A4纸大小
            Bitmap newPic = new Bitmap(W, H + A4H);
            newPic.SetResolution(gbXResolution, gbYResolution);
            using (Graphics g = Graphics.FromImage(newPic)) //续一页A4纸
            {
                g.FillRectangle(Brushes.White, 0, 0, W, H + A4H);
                g.DrawImage(imageClone, 0, 0);//把原先的图贴上                
            }
            gbOutputPicture = (Bitmap)newPic.Clone();
        }

        public void cutPicture()
        {
            Image imageClone = (Image)gbOutputPicture.Clone();
            int H = (int)(gbYPos + 0.5) + (int)(gbDeltaY + 0.5);
            int W = maxXpos.Max() + 20;//比X轴最大值宽20个像素
            if (W > 2000)
            { W = 2000; }//限定图片宽2000pixal
            Bitmap newPic = new Bitmap(W, H);
            newPic.SetResolution(gbXResolution, gbYResolution);
            using (Graphics g = Graphics.FromImage(newPic))
            {
                g.DrawImage(imageClone, new Rectangle(0, 0, W, H), new Rectangle(0, 0, W, H), GraphicsUnit.Pixel);
            }
            //g.Dispose();
            //return newPic;
            gbOutputPicture = (Bitmap)newPic.Clone();
        }

        //清空按钮
        private void btnClear_Click(object sender, EventArgs e)
        {
            txtReceive.Text = "";       //清空文本
            gbOutputPicture = new Bitmap(1, 1);//定义一个非常小的输出位图
            reportPictureBox.Image = gbOutputPicture;//输出界面
            gbOutputPicture = new Bitmap(gbImgWidth, gbImgHeigth);//定义一张A4纸的大小的图片
            Image imageClone = (Image)gbOutputPicture.Clone();
            using (Graphics g = Graphics.FromImage(imageClone))
            {
                g.FillRectangle(Brushes.White, 0, 0, gbImgWidth, gbImgHeigth);
            }
            gbOutputPicture = (Bitmap)imageClone.Clone();
            gbOutputPicture.SetResolution(gbXResolution, gbYResolution);
            gbXPos = 0;
            gbYPos = 0;
            tsFrame.Text = "当前缓存数据: 0 Byte|";
            //tsTotalData.Text = "总接收数据: 0 Byte|";
            tsTips.Text = "状态:就绪！";
            m1 = 0;
            //intTsTotalData = 0;
        }



        //关闭时事件
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            INIFILE.Profile.SaveProfile();
            sp1.Close();
        }



        private void btnSave_Click(object sender, EventArgs e)
        {

            //设置各“串口设置”
            string strBaudRate = cbBaudRate.Text;
            string strDateBits = cbDataBits.Text;
            string strStopBits = cbStop.Text;
            Int32 iBaudRate = Convert.ToInt32(strBaudRate);
            Int32 iDateBits = Convert.ToInt32(strDateBits);

            Profile.G_BAUDRATE = iBaudRate + "";       //波特率
            Profile.G_DATABITS = iDateBits + "";       //数据位
            switch (cbStop.Text)            //停止位
            {
                case "1":
                    Profile.G_STOP = "1";
                    break;
                case "1.5":
                    Profile.G_STOP = "1.5";
                    break;
                case "2":
                    Profile.G_STOP = "2";
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }
            switch (cbParity.Text)             //校验位
            {
                case "无":
                    Profile.G_PARITY = "NONE";
                    break;
                case "奇校验":
                    Profile.G_PARITY = "ODD";
                    break;
                case "偶校验":
                    Profile.G_PARITY = "EVEN";
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }

            //保存设置
            // public static string G_BAUDRATE = "1200";//给ini文件赋新值，并且影响界面下拉框的显示
            //public static string G_DATABITS = "8";
            //public static string G_STOP = "1";
            //public static string G_PARITY = "NONE";
            Profile.SaveProfile();
        }

        private void cbDataBits_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void saveTxt_Click(object sender, EventArgs e)
        {
            if (btnSetOrTrip.Text == "切换至图片格式")
            {
                if (txtReceive.Text.Length == 0)
                {
                    MessageBox.Show("Error：无报文内容!请先接收数据", "Error");
                }
                else
                {
                    System.Windows.Forms.SaveFileDialog objSave = new System.Windows.Forms.SaveFileDialog();
                    objSave.Filter = "(*.txt)|*.txt|" + "(*.*)|*.*";
                    objSave.FileName = "xx站xx装置打印报文_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".txt";
                    if (objSave.ShowDialog() == DialogResult.OK)
                    {
                        string txtFile = txtReceive.Text;
                        txtFile = txtFile.Replace("\n", "\r\n");
                        StreamWriter FileWriter = new StreamWriter(objSave.FileName, true); //写文件
                        FileWriter.Write(txtFile);//将字符串写入
                        FileWriter.Close(); //关闭StreamWriter对象
                    }
                }
            }
            else if (btnSetOrTrip.Text == "切换至文字格式")
            {
                bool isSave = true;
                SaveFileDialog saveImageDialog = new SaveFileDialog();
                saveImageDialog.Title = "图片保存";
                saveImageDialog.Filter = @"bmp|*.bmp|jpeg|*.jpg|png|*.png";

                if (saveImageDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveImageDialog.FileName.ToString();

                    if (fileName != "" && fileName != null)
                    {
                        string fileExtName = fileName.Substring(fileName.LastIndexOf(".") + 1).ToString();

                        System.Drawing.Imaging.ImageFormat imgformat = null;

                        if (fileExtName != "")
                        {
                            switch (fileExtName)
                            {
                                case "jpg":
                                    imgformat = System.Drawing.Imaging.ImageFormat.Jpeg;
                                    break;
                                case "bmp":
                                    imgformat = System.Drawing.Imaging.ImageFormat.Bmp;
                                    break;
                                case "png":
                                    imgformat = System.Drawing.Imaging.ImageFormat.Png;
                                    break;
                                default:
                                    MessageBox.Show("只能存取为: jpg,bmp,png 格式");
                                    isSave = false;
                                    break;
                            }
                        }
                        if (isSave) { reportPictureBox.Image.Save(fileName, imgformat); }
                    }
                }

            }
        }

        //打开串口按钮
        private void btnOpenCloseSCom_Click(object sender, EventArgs e)
        {
            //serialPort1.IsOpen
            if (!sp1.IsOpen)
            {
                try
                {
                    //设置串口号
                    string serialName = cbSerial.SelectedItem.ToString();
                    sp1.PortName = serialName;

                    //设置各“串口设置”
                    string strBaudRate = cbBaudRate.Text;
                    string strDateBits = cbDataBits.Text;
                    string strStopBits = cbStop.Text;
                    Int32 iBaudRate = Convert.ToInt32(strBaudRate);
                    Int32 iDateBits = Convert.ToInt32(strDateBits);

                    sp1.BaudRate = iBaudRate;       //波特率
                    sp1.DataBits = iDateBits;       //数据位
                    switch (cbStop.Text)            //停止位
                    {
                        case "1":
                            sp1.StopBits = StopBits.One;
                            break;
                        case "1.5":
                            sp1.StopBits = StopBits.OnePointFive;
                            break;
                        case "2":
                            sp1.StopBits = StopBits.Two;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }
                    switch (cbParity.Text)             //校验位
                    {
                        case "无":
                            sp1.Parity = Parity.None;
                            break;
                        case "奇校验":
                            sp1.Parity = Parity.Odd;
                            break;
                        case "偶校验":
                            sp1.Parity = Parity.Even;
                            break;
                        default:
                            MessageBox.Show("Error：参数不正确!", "Error");
                            break;
                    }

                    if (sp1.IsOpen == true)//如果打开状态，则先关闭一下
                    {
                        sp1.Close();
                    }
                    //状态栏设置
                    //tsSpNum.Text = "串口号：" + sp1.PortName + "|";
                    //tsBaudRate.Text = "波特率：" + sp1.BaudRate + "|";
                    //tsDataBits.Text = "数据位：" + sp1.DataBits + "|";
                    //tsStopBits.Text = "停止位：" + sp1.StopBits + "|";
                    //tsParity.Text = "校验位：" + sp1.Parity + "|";

                    //设置必要控件不可用
                    cbSerial.Enabled = false;
                    cbBaudRate.Enabled = false;
                    cbDataBits.Enabled = false;
                    cbStop.Enabled = false;
                    cbParity.Enabled = false;
                    btnSetOrTrip.Enabled = false; //串口打开期间不允许切换输出模式

                    sp1.Open();     //打开串口

                    btnOpenCloseSCom.Text = "关闭串口";
                    //打开定时器
                    taskTimer.Enabled = true;


                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error:" + ex.Message, "Error");
                    tmSend.Enabled = false;
                    return;
                }
            }
            else
            {

                //恢复控件功能
                //设置必要控件不可用
                cbSerial.Enabled = true;
                cbBaudRate.Enabled = true;
                cbDataBits.Enabled = true;
                cbStop.Enabled = true;
                cbParity.Enabled = true;
                btnSetOrTrip.Enabled = true; //恢复允许切换输出模式
                sp1.Close();                    //关闭串口
                btnOpenCloseSCom.Text = "打开串口";
                // tmSend.Enabled = false;         //关闭计时器
                taskTimer.Enabled = false;//关闭计时器
            }
        }



        private void btnSetOrTrip_Click(object sender, EventArgs e)
        {
            if (gbReportTxtOrBmp == true)
            {
                btnSetOrTrip.Text = "切换至文字格式";
                gbReportTxtOrBmp = false;
                tabSwitch.Controls.Clear();
                tabSwitch.Controls.Add(tabPage2);
                tabSwitch.Visible = true;
            }
            else
            {

                btnSetOrTrip.Text = "切换至图片格式";
                gbReportTxtOrBmp = true;
                tabSwitch.Controls.Clear();
                tabSwitch.Controls.Add(tabPage1);
                tabSwitch.Visible = true;
            }
        }

        static void Delay(uint ms)
        {
            uint start = GetTickCount();
            while (GetTickCount() - start < ms)
            {
                Application.DoEvents();
            }
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            btnSetOrTrip.Text = "切换至图片格式";
            gbReportTxtOrBmp = true;
            tabSwitch.Controls.Clear();
            tabSwitch.Controls.Add(tabPage1);
            tabSwitch.Visible = true;
            //更新日志
            string updateStr = "2017/08/04 完成许继电气SDR系列保护定值的规约适配" + "\r\n" +
                               "2017/01/10 完成南瑞继保RCS/PCS系列动作报告的规约适配" + "\r\n" +
                               "2016/12/27 完成南瑞继保RCS/PCS系列保护定值的规约适配";
            string teamStr = "\r\n" + "\r\n" + "本软件系统由中国南方电网超高压输电公司广州局开发";
            txtReceive.Text = updateStr + teamStr;
        }
    }
}


