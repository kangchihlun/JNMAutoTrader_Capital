using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using OSCommonLib;
using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Client;
using Hik.Communication.Scs.Communication;
namespace OSQuoteTester
{
    public partial class ClientMain : Form
    {
        public static QuoteProgramModeOS quoteProgramMode = QuoteProgramModeOS.QPM_Neutural;

        #region Define Variable
        //----------------------------------------------------------------------
        // Define Variable
        //----------------------------------------------------------------------
        private int         m_nCode;
        private string      m_strLoginID;  

        private int         m_nBest5Count;

        private delegate void InvokeSendMessage(string state);
        private delegate void InvokeHisTick(short sStockidx, int nPtr, int nTime, int nClose, int nQty);
        private delegate void InvokeTick(short sStockIdx, int nPtr);
        private delegate void InvokeBest5(short sStockidx);
        private delegate void reConnectToQuoteServer(string state);
        private delegate void ConnectToQuoteServer(object sender, EventArgs e);
        
        private Logger      m_Logger;

        private delegate void InvokeQuoteUpdate(FOREIGN Foreign);

        #region Timer Update delegate
        delegate void UpdateControl(Control Ctrl, string Msg);
        private object _objLock = new object();
        void _mUpdateControl(Control Ctrl, string Msg)
        {
            lock (this._objLock)
            {
                if (Ctrl is Label) // 更新Label文字
                {
                    ((Label)Ctrl).Text = Msg;
                }
                else if (Ctrl is ListBox)
                {
                    ((ListBox)Ctrl).Items.Add(Msg);    
                }
            }
        }
        #endregion

        private DataTable   m_dtForeigns;
        private DataTable   m_dtBest5Ask; 
        private DataTable   m_dtBest5Bid; 

        FOnConnect          fConnect;
        FOnGetStockIdx      fQuoteUpdate;
        FOnGetStockIdx      fOnNotifyBest5;
        FOnNotifyTicks      fNotifyTicks;
        FOnOverseaProducts  fOverseaProducts;
        FOnNotifyServerTime fNotifyServerTime;
        FOnNotifyKLineData  fNotifyKLineData;
        FOnNotifyTicksGet   fOnNotifyTicksGet;

        IScsServiceClient<IOSQuoteService> client;

        // 小日經熱門月
        private DateTime OSEHotDate = new DateTime();

        // 小日經熱門月  上午商品字串(群益)
        public static string OSEJNI_ID_CAP;

        // 小日經熱門月  下午商品字串(群益)
        public static string OSEJNIPM_ID_CAP;

        //上次callback 收到的時間
        private DateTime LastCbTime = new DateTime();

        //海期所有商品清單
        private List<string> prodStrCol = new List<string>();

        //小日經1分K昨日歷史資料(文字)
        private List<KLineStr> OseJniHistKL1MinStr = new List<KLineStr>();

        // 程式啟動的時間
        private DateTime programStartTime = new DateTime();


        List<string> tickLog = new List<string>();

        public TICK m_Tick;

        //今天日期
        DateTime todate = DateTime.Now;

        //上午開盤時間，日本時間上午9:00
        public DateTime g_marketStartTime_AM;

        //上午收盤時間，日本時間下午3:15
        public DateTime g_marketEndTime_AM;

        //下午開盤時間，日本時間下午2:30
        public DateTime g_marketStartTime_PM;

        //下午收盤時間，日本時間上午3:00
        public DateTime g_marketEndTime_PM;

        //上午收盤前10分，啟動Timer時間
        public DateTime g_marketNearEndTime_AM;

        //下午收盤前10分，啟動Timer時間
        public DateTime g_marketNearEndTime_PM;

        //Tick報價會斷，嘗試重連
        bool bReconnect = false;

        
        #endregion

        #region Initialize
        //----------------------------------------------------------------------
        // Initialize
        //----------------------------------------------------------------------
        public ClientMain(string[] args)
        {
            //開啟之後特殊模式判別，雙擊開啟不會有任何特殊模式
            string strMode = "";
            int iMode = 0;
            try
            {
                strMode = args[0];
                iMode = int.Parse(strMode);
            }
            catch { }
            switch (iMode)
            {
                case 0: quoteProgramMode = QuoteProgramModeOS.QPM_Neutural;
                    Console.WriteLine("當前模式為 Client 一般雙擊開啟");
                    break;
                case 1: quoteProgramMode = QuoteProgramModeOS.QPM_AllProduct;
                    Console.WriteLine("當前模式為  Server 取得當前商品列表");
                    break;
                case 3: quoteProgramMode = QuoteProgramModeOS.QPM_MarketAM;
                    Console.WriteLine("當前模式為  Client  小日經上午盤中作單模式");
                    break;
                case 4: quoteProgramMode = QuoteProgramModeOS.QPM_AMMarketTickGet;
                    Console.WriteLine("當前模式為  Client  小日經上午盤後Tick資訊擷取，上午倉位紀錄");
                    break;
                case 5: quoteProgramMode = QuoteProgramModeOS.QPM_MarketPM;
                    Console.WriteLine("當前模式為  Client  小日經下午盤作單模式");
                    break;
                case 6: quoteProgramMode = QuoteProgramModeOS.QPM_AfterMarket;
                    Console.WriteLine("當前模式為  Client  小日經下午盤後Tick資訊擷取，寫入每日歷史紀錄");
                    break;
            }
            

            InitializeComponent();

            fConnect = new FOnConnect(OnConnect);
            GC.KeepAlive(fConnect);

            fQuoteUpdate = new FOnGetStockIdx(OnQuoteUpdate);
            GC.KeepAlive(fQuoteUpdate);

            fNotifyTicks = new FOnNotifyTicks(OnNotifyTicks);
            GC.KeepAlive(fNotifyTicks);

            fOnNotifyBest5 = new FOnGetStockIdx(OnNotifyBest5);
            GC.KeepAlive(fOnNotifyBest5);

            fOverseaProducts = new FOnOverseaProducts(OnOverseaProducts);
            GC.KeepAlive(fOverseaProducts);

            fNotifyServerTime = new FOnNotifyServerTime(OnNotifyServerTime);
            GC.KeepAlive(fNotifyServerTime);

            fNotifyKLineData = new FOnNotifyKLineData(OnNotifyKLineData);
            GC.KeepAlive(fNotifyKLineData);

            fOnNotifyTicksGet = new FOnNotifyTicksGet(OnNotifyTicksGet);
            GC.KeepAlive(fOnNotifyTicksGet);

            m_Logger = new Logger();

            client = ScsServiceClientBuilder.CreateClient<IOSQuoteService>(new ScsTcpEndPoint("127.0.0.1", 10083));


            g_marketStartTime_AM = new DateTime(todate.Year, todate.Month, todate.Day, 9, 0, 0);

            g_marketEndTime_AM = new DateTime(todate.Year, todate.Month, todate.Day, 15, 15, 59);

            g_marketStartTime_PM = new DateTime(todate.Year, todate.Month, todate.Day, 16, 29, 59);

            DateTime nxDate = todate.AddDays(1);
            g_marketEndTime_PM = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 3, 0, 0);

            //上午收盤前10分，啟動timer時間
            g_marketNearEndTime_AM = g_marketEndTime_AM.AddMinutes(-10);

            //上午收盤前10分，啟動timer時間
            g_marketNearEndTime_PM = g_marketEndTime_PM.AddMinutes(-10);

            this.tabControl1.SelectTab(1);
        }
        #endregion

        #region Component Event
        //----------------------------------------------------------------------
        // Component Event
        //----------------------------------------------------------------------
        private void Main_Load(object sender, EventArgs e)
        {
            m_nBest5Count = 0;

            lblConnect.ForeColor = Color.Red;

            boxKLineType.SelectedIndex = 0;

            m_dtForeigns    = CreateStocksDataTable();
            m_dtBest5Ask    = CreateBest5AskTable();
            m_dtBest5Bid    = CreateBest5AskTable();

            SetDoubleBuffered(gridStocks);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_nCode = Functions.SKOSQuoteLib_LeaveMonitor();
        }

        private void btnInitialize_Click(object sender, EventArgs e)
        {
            string strPassword;

            m_strLoginID = txtAccount.Text.Trim();
            strPassword = txtPassWord.Text.Trim();

            // 取得商品名
            readHistoryKLData();

            //Initialize SKOrderLib
            m_nCode = Functions.SKOSQuoteLib_Initialize(m_strLoginID, strPassword);

            //m_Logger.Write("SKOSQuoteLib_Initialize  Code：" + m_nCode.ToString());

           

            m_nCode = Functions.SKOSQuoteLib_AttachConnectCallBack(fConnect);

            m_nCode = Functions.SKOSQuoteLib_AttachQuoteCallBack(fQuoteUpdate);

            m_nCode = Functions.SKOSQuoteLib_AttachTicksCallBack(fNotifyTicks);

            m_nCode = Functions.SKOSQuoteLib_AttachBest5CallBack(fOnNotifyBest5);

            m_nCode = Functions.SKOSQuoteLib_AttachServerTimeCallBack(fNotifyServerTime);

            m_nCode = Functions.SKOSQuoteLib_AttachHistoryTicksGetCallBack(fOnNotifyTicksGet);

            if (m_nCode == 0)
            {
                Console.WriteLine("Initialize Success");
                lblMessage.Text = "元件初始化完成";
                btnConnect_Click(sender, e);
            }
            else if (m_nCode == 2003)
            {
                lblMessage.Text = "元件已初始過，無須重複執行";
            }
            else
            {
                lblMessage.Text = "元件初始化失敗 code " + GetApiCodeDefine(m_nCode);
                return;
            }
        }

        public void btnConnect_Click(object sender, EventArgs e)
        {
            m_nCode = Functions.SKOSQuoteLib_EnterMonitor(0);

            if (m_nCode != 0)
            {
                lblMessage.Text = "連線失敗 code " + GetApiCodeDefine(m_nCode);
            }
            else
            {
                if (client.CommunicationState == CommunicationStates.Disconnected)
                {
                    if ((quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) || (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
                        client.Connect();
                }
            }

            //
            //btnGo_Click(sender,e);
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            m_nCode = Functions.SKOSQuoteLib_LeaveMonitor();

            if (m_nCode != 0)
            {
                //lblMessage.Text = "斷線失敗 code " + GetApiCodeDefine(m_nCode);
            }

            //if(bReconnect)
            //{
            //    btnConnect_Click(sender, e);
            //}
            //m_Logger.Write("SKOSQuoteLib_LeaveMonitor Code:" + m_nCode.ToString());
        }

        private void btnQueryStocks_Click(object sender, EventArgs e)
        {
            string strStocks;
            int nPage = 0;

            m_dtForeigns.Clear();
            gridStocks.ClearSelection();

            gridStocks.DataSource = m_dtForeigns;


            gridStocks.Columns["m_sStockidx"].Visible       = false;
            gridStocks.Columns["m_sDecimal"].Visible        = false;
            gridStocks.Columns["m_nDenominator"].Visible    = false;
            gridStocks.Columns["m_cMarketNo"].Visible       = false;
            gridStocks.Columns["m_caExchangeNo"].HeaderText = "交易所代碼";
            gridStocks.Columns["m_caExchangeName"].HeaderText = "交易所名稱";
            gridStocks.Columns["m_caStockNo"].HeaderText    = "商品代碼";
            gridStocks.Columns["m_caStockName"].HeaderText  = "商品名稱";

            gridStocks.Columns["m_nOpen"].HeaderText        = "開盤價";
            gridStocks.Columns["m_nHigh"].HeaderText        = "最高價";
            gridStocks.Columns["m_nLow"].HeaderText         = "最低價";
            gridStocks.Columns["m_nClose"].HeaderText       = "成交價";
            gridStocks.Columns["m_dSettlePrice"].HeaderText = "結算價";
            gridStocks.Columns["m_nTickQty"].HeaderText     = "單量";
            gridStocks.Columns["m_nRef"].HeaderText         = "昨收價";

            gridStocks.Columns["m_nBid"].HeaderText         = "買價";
            gridStocks.Columns["m_nBc"].HeaderText          = "買量";
            gridStocks.Columns["m_nAsk"].HeaderText         = "賣價";
            gridStocks.Columns["m_nAc"].HeaderText          = "賣量";
            gridStocks.Columns["m_nTQty"].HeaderText        = "成交量"; 



            strStocks = txtStocks.Text.Trim();

            m_nCode = Functions.SKOSQuoteLib_RequestStocks(out nPage, strStocks);

            //m_Logger.Write("SKOSQuoteLib_RequestStocks Page:" + nPage.ToString() + " Stocks[" + strStocks + "]" + " Code:" + m_nCode.ToString());

            if (m_nCode != 0)
            {
                lblMessage.Text = "查詢商品失敗 code " + GetApiCodeDefine(m_nCode);
            }

            gridStocks.Refresh();
        }

        private void btnTick_Click(object sender, EventArgs e)
        {
            listTicks.Items.Clear();
            m_dtBest5Ask.Clear();
            m_dtBest5Bid.Clear();

            gridBest5Bid.DataSource = m_dtBest5Bid;
            gridBest5Ask.DataSource = m_dtBest5Ask;


            gridBest5Ask.Columns["m_nAskQty"].HeaderText = "張數";
            gridBest5Ask.Columns["m_nAskQty"].Width = 60;
            gridBest5Ask.Columns["m_nAsk"].HeaderText = "賣價";
            gridBest5Ask.Columns["m_nAsk"].Width = 60;

            gridBest5Bid.Columns["m_nAskQty"].HeaderText = "張數";
            gridBest5Bid.Columns["m_nAskQty"].Width = 60;
            gridBest5Bid.Columns["m_nAsk"].HeaderText = "買價";
            gridBest5Bid.Columns["m_nAsk"].Width = 60;


            int nPage = 0;

            string strTicks = txtTick.Text.Trim();

            m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);

            //m_Logger.Write("SKOSQuoteLib_RequestTicks Code:" + m_nCode.ToString() + "tick[" + strTicks+"]");

            if (m_nCode != 0)
            {
                lblMessage.Text = "查詢Ticks失敗 code " + GetApiCodeDefine(m_nCode);
            }

        }

        private void gridStocks_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                e.Graphics.FillRectangle(Brushes.Black, e.CellBounds);

                if (e.Value != null)
                {
                    string strHeaderText = ((DataGridView)sender).Columns[e.ColumnIndex].HeaderText.ToString();

                    int nDenominator = int.Parse(((DataGridView)sender).Rows[e.RowIndex].Cells["m_nDenominator"].Value.ToString()); 

                    if (strHeaderText == "名稱")
                    {
                        e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, Brushes.SkyBlue, e.CellBounds.X, e.CellBounds.Y);
                    }
                    else if (strHeaderText == "買價" || strHeaderText == "賣價" || strHeaderText == "成交價" || strHeaderText == "開盤價" || strHeaderText == "最高價" || strHeaderText == "最低價" || strHeaderText == "昨收價")
                    {
                        double dPrc = double.Parse(((DataGridView)sender).Rows[e.RowIndex].Cells["m_nRef"].Value.ToString());

                        double dValue = double.Parse(e.Value.ToString());

                        string strCellValue = "";


                        if (nDenominator > 1)
                        {
                            string strValue = e.Value.ToString();

                            if (strValue.IndexOf('.') > -1)
                            {
                                int nValue1 = int.Parse(strValue.Substring(0, strValue.IndexOf('.')));

                                double dValue2 = double.Parse(strValue.Substring(strValue.IndexOf('.'), (strValue.Length - strValue.IndexOf('.') )));

                                strCellValue = nValue1.ToString() + "'" + (dValue2 * nDenominator).ToString("#0.##");
                            }
                        }
                        else
                        {
                            strCellValue = e.Value.ToString();
                        }

                        if (dValue > dPrc)
                        {
                            e.Graphics.DrawString(strCellValue, e.CellStyle.Font, Brushes.Red, e.CellBounds.X, e.CellBounds.Y);

                        }
                        else if (dValue < dPrc)
                        {
                            e.Graphics.DrawString(strCellValue, e.CellStyle.Font, Brushes.Lime, e.CellBounds.X, e.CellBounds.Y);
                        }
                        else
                        {
                            e.Graphics.DrawString(strCellValue, e.CellStyle.Font, Brushes.White, e.CellBounds.X, e.CellBounds.Y);
                        }
                    }
                    else if (strHeaderText == "單量" || strHeaderText == "成交量")
                    {
                        e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, Brushes.Yellow, e.CellBounds.X, e.CellBounds.Y);
                    }
                    else
                    {
                        e.Graphics.DrawString(e.Value.ToString(), e.CellStyle.Font, Brushes.White, e.CellBounds.X, e.CellBounds.Y);
                    }
                }
                e.Handled = true;
            }
        }

        private void btnOverseaProducts_Click(object sender, EventArgs e)
        {
            listOverseaProducts.Items.Clear();

            m_nCode = Functions.SKOSQuoteLib_AttachOverseaProductsCallBack(fOverseaProducts);

            m_nCode = Functions.SKOSQuoteLib_RequestOverseaProducts();
        }

        private void btnServerTime_Click(object sender, EventArgs e)
        {
            m_nCode = Functions.SKOSQuoteLib_RequestServerTime();

            if (m_nCode != 0)
            {
                lblMessage.Text = "查詢時間失敗 code " + GetApiCodeDefine(m_nCode);
            }
        }

        private void btnKLine_Click(object sender, EventArgs e)
        {
            listKLine.Items.Clear();

            string  strStock    = "";
            int     nType       = 0;

            strStock = txtKLine.Text.Trim();
            nType    = boxKLineType.SelectedIndex;

            m_nCode = Functions.SKOSQuoteLib_AttachKLineDataCallBack(fNotifyKLineData);

            m_nCode = Functions.SKOSQuoteLib_RequestKLine(strStock,(short)nType);

        }

        private void saveLog_AM()
        {
            string fileSavePath = Application.StartupPath + @"\AMTicks.xml";
            DateTime lastWriteTime = File.GetLastWriteTime(fileSavePath);
            if ( (lastWriteTime.Day != todate.Day)&&( DateTime.Compare( todate, lastWriteTime ) > 0) )
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileSavePath, false, System.Text.Encoding.ASCII))
                {
                    //寫入上午Tick
                    foreach (string line in tickLog)
                    {
                        file.WriteLine(line);
                    }
                }
            }
        }

        private void saveLog_PM()
        {
            //現在是隔天的凌晨2點
            string amtkPath = Application.StartupPath + @"\AMTicks.xml";
            DateTime lastWriteTime = File.GetLastWriteTime(amtkPath);
            DateTime prevDay = DateTime.Now.AddDays(-1);
            if (prevDay.Day == lastWriteTime.Day) //AMLog在昨天下午有紀錄
            {
                System.IO.DirectoryInfo appRoot = Directory.GetParent(Directory.GetParent(Application.StartupPath).FullName);
                string fileSavePath = appRoot.FullName + @"\JNMHistories\JNMTickLog[";
                DateTime prvDate = DateTime.Now.AddDays(-1);
                fileSavePath += prvDate.Month.ToString() + "-";
                if (prvDate.Day < 10)
                    fileSavePath += "0" + prvDate.Day.ToString() + "-";
                else
                    fileSavePath += prvDate.Day.ToString() + "-";
                fileSavePath += prvDate.Year.ToString() + "].xml";

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileSavePath, false, System.Text.Encoding.ASCII))
                {
                    // 讀寫入歷史1分K
                    string ff = Application.StartupPath + @"\HistoryKLine1Min.xml";
                    using (StreamReader sr = new StreamReader(ff, Encoding.Default, true))
                    {
                        while (sr.Peek() != -1)
                        {
                            string _tkstr = sr.ReadLine();
                            file.WriteLine(_tkstr);
                        }
                    }
                    // 讀寫上午Tick
                    ff = Application.StartupPath + @"\AMTicks.xml";
                    using (StreamReader sr = new StreamReader(ff, Encoding.Default, true))
                    {
                        while (sr.Peek() != -1)
                        {
                            string _tkstr = sr.ReadLine();
                            file.WriteLine(_tkstr);
                        }
                    }
                    //最後寫入下午Tick
                    foreach (string line in tickLog)
                    {
                        file.WriteLine(line);
                    }
                }
            }
        }


        #region 時間處理函式
        public DateTime convertJpnTimeToTw(DateTime _in)
        {
            return (_in.AddHours(-1));
        }

        public DateTime convertTwTimeToJpn(DateTime _in)
        {
            return (_in.AddHours(1));
        }

        #endregion

        private void timer2_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            int remainTm = 0;
            switch (quoteProgramMode)
            {
                case QuoteProgramModeOS.QPM_MarketAM://小日經上午盤中作單模式
                {
                    remainTm = (int)(convertJpnTimeToTw(g_marketEndTime_AM) - DateTime.Now).TotalSeconds;
                }
                break;
                case QuoteProgramModeOS.QPM_MarketPM://小日經下午盤中作單模式
                {
                    remainTm = (int)(convertJpnTimeToTw(g_marketEndTime_PM) - DateTime.Now).TotalSeconds;
                }
                break;
            }
            TimeSpan t = TimeSpan.FromSeconds(remainTm);
            string msg = t.Hours.ToString() + ":" + t.Minutes.ToString() + ":" + t.Seconds.ToString();
            this.BeginInvoke(new UpdateControl(_mUpdateControl), new object[] { this.lblServerTime, msg });



            switch (quoteProgramMode)
            {
                #region QPM_AllProduct
                case (QuoteProgramModeOS.QPM_AllProduct):
                    {
                        if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                        if (prodStrCol.Count < 100) break;
                        List<DateTime> prodOSEHot3 = new List<DateTime>();
                        DateTime _todate = DateTime.Now;
                        string jpnPrefix = "JNM";
                        foreach (string strProducts in prodStrCol)
                        {
                            //Console.WriteLine(strProducts.Trim());
                            string[] prodFulNm = (strProducts.Trim()).Split(',');
                            if (prodFulNm[0].ToUpper().IndexOf("OSE") >= 0)
                            {
                                string stID = prodFulNm[2];
                                if ((stID.ToUpper().IndexOf("JNM") == 0) && (stID.ToUpper().IndexOf("PM") == -1)) //
                                {
                                    jpnPrefix = stID.Substring(0, 3);
                                    int _my = int.Parse(stID.Substring(3, 4));
                                    if (_my > 1000)
                                    {
                                        int yr = _my / 100;
                                        int mth = _my - yr * 100;

                                        DateTime dDate = new DateTime(2000 + yr, mth, _todate.Day);
                                        prodOSEHot3.Add(dDate);
                                    }
                                }
                            }
                        }

                        // Note.這邊改為交易下個熱門月，這樣就完全沒有換倉問題了
                        // e.g. 目前1月，交易6月份的商品
                        int days = 100; //當前時間到目前熱門月結算最多100天(大約3個月)
                        for(int k = 0;k<prodOSEHot3.Count;k++){
                            DateTime da = prodOSEHot3[k];
                            int nd = (int)(da - _todate).TotalDays;
                            if (nd < days){
                                if (k + 1 <= prodOSEHot3.Count){
                                    OSEHotDate = prodOSEHot3[k + 1];
                                    days = nd;
                                }
                            }
                        }

                        if( OSEHotDate.Year < 2) // 至少要有值
                            OSEHotDate = prodOSEHot3[0];

                        #region 檢查是否準備換倉[目前無用]
                        // 小日經結算日 各合約月份第二個星期五之前一營業日(如遇假日則提前一天)
                        // 每月份第二個星期三，最快出現在8號
                        DateTime _secondWendsday = new DateTime(OSEHotDate.Year, OSEHotDate.Month, 8);
                        while (_secondWendsday.DayOfWeek != DayOfWeek.Wednesday)
                        {
                            _secondWendsday = _secondWendsday.AddDays(1);
                        }
                        #endregion


                        string strOSEHotYM = ((OSEHotDate.Year - 2000) * 100 + OSEHotDate.Month).ToString();
                        OSEJNI_ID_CAP = jpnPrefix + strOSEHotYM;
                        OSEJNIPM_ID_CAP = jpnPrefix + "PM" + strOSEHotYM;

                        if ((OSEJNI_ID_CAP.Length > 0) && (OSEJNI_ID_CAP.Length > 0))
                        {
                            Console.WriteLine("熱門月  " + OSEJNI_ID_CAP);
                            quoteProgramMode = QuoteProgramModeOS.QPM_MarketKLGetter;

                            // 開始收集K棒
                            m_nCode = Functions.SKOSQuoteLib_AttachKLineDataCallBack(fNotifyKLineData);
                            m_nCode = Functions.SKOSQuoteLib_RequestKLine("OSE," + OSEJNI_ID_CAP, (short)0);
                            m_nCode = Functions.SKOSQuoteLib_RequestKLine("OSE," + OSEJNIPM_ID_CAP, (short)0);
                        }
                    }
                    break;
                #endregion
                #region QPM_MarketKLGetter
                case (QuoteProgramModeOS.QPM_MarketKLGetter):
                    {
                        if (OseJniHistKL1MinStr.Count < 100) break;
                        if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                        // K棒時間排序
                        List<KLineStr> historyKLine_Day_tSorted = OseJniHistKL1MinStr.OrderBy(c => c.date).ToList();

                        string fileSavePath = Application.StartupPath + @"\HistoryKLine1Min.xml";
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileSavePath, false, System.Text.Encoding.ASCII))
                        {
                            file.WriteLine(OSEJNI_ID_CAP);
                            file.WriteLine(OSEJNIPM_ID_CAP);
                            // 寫入歷史1分K
                            foreach (KLineStr line in historyKLine_Day_tSorted)
                            {
                                file.WriteLine(line._str);
                            }
                        }
                        Process.GetCurrentProcess().Kill();

                    }
                    break;
                #endregion
                #region 上午盤中
                case (QuoteProgramModeOS.QPM_MarketAM):
                    {
                        //if (bReconnect)
                        //{
                        //    this.BeginInvoke(new ConnectToQuoteServer(btnConnect_Click), new object[] { null,null});
                        //}

                        if (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_AM)) > 0)
                        {
                            btnDisconnect_Click(null, null);
                           // try { client.ServiceProxy.Quit(); }
                           // catch { }
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    break;
                #endregion
                #region 下午盤中
                case (QuoteProgramModeOS.QPM_MarketPM):
                    {
                        //if (bReconnect)
                        //{
                        //    this.BeginInvoke(new ConnectToQuoteServer(btnConnect_Click), new object[] { null, null });
                        //}

                        if (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_PM)) > 0)
                        {
                            btnDisconnect_Click(null, null);
                            try { client.ServiceProxy.Quit(); }
                            catch { }
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    break;
                #endregion
                #region QPM_AMMarketTickGet
                case (QuoteProgramModeOS.QPM_AMMarketTickGet):
                    {
                        if ((DateTime.Now - programStartTime).TotalSeconds < 300)
                        {
                            if (LastCbTime.Year < 2) return;//至少有一次callback
                            if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                            saveLog_AM();
                            Process.GetCurrentProcess().Kill();
                        }
                        else if (LastCbTime.Year < 2) //10分鐘內沒收到任何callback
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    break;
                #endregion
                #region QPM_AfterMarket
                case (QuoteProgramModeOS.QPM_AfterMarket):
                    {
                        if ((DateTime.Now - programStartTime).TotalSeconds < 300)
                        {
                            if (LastCbTime.Year < 2) return;//至少有一次callback
                            if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                            saveLog_PM();
                            Process.GetCurrentProcess().Kill();
                        }
                        else if (LastCbTime.Year < 2) //10分鐘內沒收到任何callback
                        {
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    break;
                #endregion

            }
            
        }
        
        #endregion

        #region CallBack Function
        //----------------------------------------------------------------------
        // CallBack Function
        //----------------------------------------------------------------------
        //Connect CallBack Function
        public void OnConnect(int nKind, int nErrorCode)
        {
            //m_Logger.Write("OnConnect CallBack 通知 nKind:" + nKind.ToString() + " nErrorCode" + nErrorCode.ToString());

            if (nKind == 100)
            {
                WriteInfo("Connect Code: " + GetApiCodeDefine(nErrorCode));
                lblConnect.ForeColor = Color.Green;
                if (bReconnect)
                {
                    if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM)
                    {
                        //小日經上午盤中作單模式
                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNI_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    else if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
                    {
                        //小日經上午盤中作單模式
                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNIPM_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    //重置
                    if (bReconnect)
                        Console.WriteLine("+++++++++ Reconnected +++++++++ Time: " + DateTime.Now.ToString());
                    bReconnect = false;
                    
                }
            }
            else if (nKind == 101)
            {
                WriteInfo("Disconnect Code: " + GetApiCodeDefine(nErrorCode));
                Console.WriteLine("+++++++++ Disconnected +++++++++ Time: " + DateTime.Now.ToString());
                lblConnect.ForeColor = Color.Red;
                //if(!this.bReconnect)
                //    this.Invoke(new reConnectToQuoteServer(this.ReConnectQuoteServer), new object[] { nKind.ToString() });
                // 重連，比較時間

                if (!bReconnect)
                {
                    if ((DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketStartTime_AM)) > 0) &&
                       (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_PM)) < 0))
                    {
                        bReconnect = true;
                        btnConnect_Click(null, null);
                    }
                }
                
            }
        }

        public void ReConnectQuoteServer(string strMsg)
        {
            // 重連，比較時間
            bool bTimeToReconnect = false;
            if ((quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) && (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_AM)) < 0))
                bTimeToReconnect = true;
            if ((quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM) && (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_PM)) < 0))
                bTimeToReconnect = true;
            if (bTimeToReconnect)
                bReconnect = true;
        }

        public void OnQuoteUpdate(short nStockIdx)
        {
            FOREIGN Foreign;

            m_nCode = Functions.SKOSQuoteLib_GetStockByIndex(nStockIdx, out Foreign);

            if (m_nCode != 0)
            {
                lblMessage.Text = "商品報價查詢失敗 " + GetApiCodeDefine(m_nCode);
            }
            else
            {
                this.Invoke(new InvokeQuoteUpdate(this.OnUpDateDataQuote), new object[] { Foreign });
            }
        }

        public void OnNotifyTicks( short sStockIdx , int nPtr )
        {
            //m_Logger.Write("OnQuoteUpdate CallBack 通知 sStockIdx:" + sStockIdx.ToString() + " nPtr:" + nPtr.ToString());

            if (listTicks.InvokeRequired == true)
            {
                this.Invoke(new InvokeTick(this.OnNotifyTicks), new object[] { sStockIdx, nPtr });
            }
            else
            {
                TICK tTick;

                m_nCode = Functions.SKQuoteLib_GetTick(sStockIdx, nPtr, out tTick);

                if (m_nCode == 0)
                {
                    listTicks.Items.Add("時間：" + tTick.m_nTime.ToString() + "  成交價：" + tTick.m_nClose.ToString() + "  量：" + tTick.m_nQty.ToString());

                    listTicks.SelectedIndex = listTicks.Items.Count - 1;

                    if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) 
                    {
                        LastCbTime = DateTime.Now;
                        client.ServiceProxy.OnNotifyTicks(tTick);

                        if (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketNearEndTime_AM)) > 0)
                        {
                            //listBox1.Items.Add("時間：" + tTick.m_nTime.ToString() + " 將近收盤，準備收盤" );
                        }
                    }
                    else if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
                    {
                        LastCbTime = DateTime.Now;
                        client.ServiceProxy.OnNotifyTicks(tTick);

                        if (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketNearEndTime_PM)) > 0)
                        {
                            listBox1.Items.Add("時間：" + tTick.m_nTime.ToString() + " 將近收盤，準備收盤");
                        }
                    }
                    
                }
            }
        }

        public void OnNotifyTicksGet( short sStockidx,int nPtr, int nTime, int nClose, int nQty)
        {

            if (listTicks.InvokeRequired == true)
            {
                this.Invoke(new InvokeHisTick(this.OnNotifyTicksGet), new object[] {  sStockidx,nPtr, nTime, nClose, nQty });
            }
            else
            {
                //string strMsg = "時間：" + nTime.ToString() + "  成交價：" + nClose.ToString() + "  量：" + nQty.ToString();

                //listTicks.Items.Add(strMsg);

                //listTicks.SelectedIndex = listTicks.Items.Count - 1;

                #region TEST SERVERSEND
                //TICK _t = new TICK();
                //_t.m_nClose = nClose;
                //_t.m_nQty = nQty;
                //_t.m_nTime = nTime;
                //_t.m_nPtr = nPtr;
                //client.ServiceProxy.OnNotifyTicks(_t);
                #endregion

                if ((quoteProgramMode == QuoteProgramModeOS.QPM_AMMarketTickGet) || (quoteProgramMode == QuoteProgramModeOS.QPM_AfterMarket))
                {
                    string tkStr = "<Tick Time:" + nTime.ToString() + "= Price:" + nClose.ToString() + " Amount:" + nQty.ToString() + " \" />";
                    tickLog.Add(tkStr);
                }
                else
                {
                    TICK tTick = new TICK();
                    tTick.m_nClose = nClose;
                    tTick.m_nPtr = nPtr;
                    tTick.m_nQty = nQty;
                    tTick.m_nTime = nTime;
                    client.ServiceProxy.OnNotifyTicksGet(tTick);
                }
                LastCbTime = DateTime.Now;
            }
            

        }

        public void OnNotifyBest5(short sStockidx)
        {
            if (lblBest5Count.InvokeRequired == true)
            {
                this.Invoke(new InvokeBest5(this.OnNotifyBest5), new object[] { sStockidx });
            }
            else
            {
                BEST5 best5;

                Functions.SKOSQuoteLib_GetBest5(sStockidx, out best5);

                InsertBest5(best5);

                m_nBest5Count += 1;

                lblBest5Count.Text = m_nBest5Count.ToString();

                if ((quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) || (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
                {
                    client.ServiceProxy.OnNotifyBest5(best5);
                    LastCbTime = DateTime.Now;
                }
            }
        }

        public void OnOverseaProducts(string strProducts)
        {
            if (quoteProgramMode == QuoteProgramModeOS.QPM_AllProduct)
            {
                LastCbTime = DateTime.Now;
                prodStrCol.Add(strProducts);
            }
            else
            {
                listOverseaProducts.Items.Add(strProducts.Trim());
            }
        }

        public void OnNotifyServerTime(short sHour, short sMinute, short sSecond)
        {
            //lblServerTime.Text = sHour.ToString() + "：" + sMinute.ToString() + "：" + sSecond.ToString();
        }

        public void OnNotifyKLineData(string strStockNo, string strKLineData)
        {
            Console.WriteLine(strKLineData);
            if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketKLGetter)
            {
                LastCbTime = DateTime.Now;
                string[] klspls = strKLineData.Split(',');
                //inbound 開2 高3 低4 收5 量6
                //outbound 開2 收5 高3 低4  量6
                string formatstr = "<:" + klspls[2] + ":" + klspls[5] + ":" + klspls[3] + ":" + klspls[4] + ":" + klspls[6] + " :" + klspls[0] + "=\"\"/>";
                KLineStr klstr = new KLineStr();
                klstr._str = formatstr;

                if (!klspls[0].Contains("#"))
                {
                    //時間取得
                    int ymd = int.Parse(klspls[0].Split(' ')[0]);
                    int year = 2015;
                    int month = 1;
                    int day = 1;
                    if (ymd > 20100000)
                    {
                        year = ymd / 10000;
                        month = (ymd - year * 10000) / 100;
                        day = (ymd - year * 10000 - month * 100);
                    }
                    int hour = int.Parse(klspls[0].Split(' ')[1].Split(':')[0]);
                    int minute = int.Parse(klspls[0].Split(' ')[1].Split(':')[1]);
                    klstr.date = new DateTime(year, month, day, hour, minute, 0);
                    OseJniHistKL1MinStr.Add(klstr);
                }
            }
            else
            {
                listKLine.Items.Add(strKLineData.Trim());
            }
        }

        #endregion

        #region Custom Method
        //----------------------------------------------------------------------
        // Custom Method
        //----------------------------------------------------------------------
        public string GetApiCodeDefine(int nCode)
        {
            string strNCode = Enum.GetName(typeof(ApiMessage), nCode);

            if (strNCode == "" || strNCode == null)
            {
                return nCode.ToString();
            }
            else
            {
                return strNCode;
            }
        }


        public void WriteInfo(string strMsg)
        {
            if (lblMessage.InvokeRequired == true)
            {
                this.Invoke(new InvokeSendMessage(this.WriteInfo), new object[] { strMsg });
            }
            else
            {
                lblMessage.Text = strMsg;
            }
        }

        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            if (System.Windows.Forms.SystemInformation.TerminalServerSession) return;

            System.Reflection.PropertyInfo aProp =
                        typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }

        DataTable CreateStocksDataTable()
        {
            DataTable myDataTable = new DataTable();

            DataColumn myDataColumn;

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int16");
            myDataColumn.ColumnName = "m_sStockidx";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int16");
            myDataColumn.ColumnName = "m_sDecimal";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int32");
            myDataColumn.ColumnName = "m_nDenominator";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.String");
            myDataColumn.ColumnName = "m_cMarketNo";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.String");
            myDataColumn.ColumnName = "m_caExchangeNo";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.String");
            myDataColumn.ColumnName = "m_caExchangeName";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.String");
            myDataColumn.ColumnName = "m_caStockNo";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.String");
            myDataColumn.ColumnName = "m_caStockName";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nOpen";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nHigh";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nLow";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nClose";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_dSettlePrice";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int32");
            myDataColumn.ColumnName = "m_nTickQty";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nRef";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nBid";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int32");
            myDataColumn.ColumnName = "m_nBc";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nAsk";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int32");
            myDataColumn.ColumnName = "m_nAc";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int64");
            myDataColumn.ColumnName = "m_nTQty";
            myDataTable.Columns.Add(myDataColumn);

            myDataTable.PrimaryKey = new DataColumn[] { myDataTable.Columns["m_sStockidx"] };

            return myDataTable;
        }

        void OnUpDateDataQuote( FOREIGN Foreign )
        {
            string strLogMsg = "";

            strLogMsg += " m_caExchangeNo:" + Foreign.m_caExchangeNo.ToString();
            strLogMsg += " m_caExchangeName:" + Foreign.m_caExchangeName.ToString();
            strLogMsg += " m_caExchangeName:" + Foreign.m_caExchangeName.ToString();
            strLogMsg += " m_caStockNo:" + Foreign.m_caStockNo.ToString();
            strLogMsg += " m_caStockName:" + Foreign.m_caStockName.ToString();

            //m_Logger.Write(strLogMsg);

            short sStockIdx = Foreign.m_sStockidx;

            DataRow drFind = m_dtForeigns.Rows.Find(sStockIdx);
            if (drFind == null)
            {
                DataRow myDataRow = m_dtForeigns.NewRow();

                myDataRow["m_sStockidx"]        = Foreign.m_sStockidx;
                myDataRow["m_sDecimal"]         = Foreign.m_sDecimal;
                myDataRow["m_nDenominator"]     = Foreign.m_nDenominator;
                myDataRow["m_cMarketNo"]        = Foreign.m_cMarketNo.Trim();
                myDataRow["m_caExchangeNo"]     = Foreign.m_caExchangeNo.Trim();
                myDataRow["m_caExchangeName"]   = Foreign.m_caExchangeName.Trim();
                myDataRow["m_caStockNo"]        = Foreign.m_caStockNo.Trim();
                myDataRow["m_caStockName"]      = Foreign.m_caStockName.Trim();

                myDataRow["m_nOpen"]            = Foreign.m_nOpen / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nHigh"]            = Foreign.m_nHigh / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nLow"]             = Foreign.m_nLow / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nClose"]           = Foreign.m_nClose / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_dSettlePrice"]     = Foreign.m_dSettlePrice / ( Math.Pow( 10, Foreign.m_sDecimal));

                myDataRow["m_nTickQty"]         = Foreign.m_nTickQty;
                myDataRow["m_nRef"]             = Foreign.m_nRef / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nBid"]             = Foreign.m_nBid / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nBc"]              = Foreign.m_nBc;
                myDataRow["m_nAsk"]             = Foreign.m_nAsk;
                myDataRow["m_nAc"]              = Foreign.m_nAc / ( Math.Pow( 10, Foreign.m_sDecimal));
                myDataRow["m_nTQty"]            = Foreign.m_nTQty;

                m_dtForeigns.Rows.Add(myDataRow);
            }
            else
            {
                drFind["m_sStockidx"]           = Foreign.m_sStockidx;
                drFind["m_sDecimal"]            = Foreign.m_sDecimal;
                drFind["m_nDenominator"]        = Foreign.m_nDenominator;
                drFind["m_cMarketNo"]           = Foreign.m_cMarketNo.Trim();
                drFind["m_caExchangeNo"]        = Foreign.m_caExchangeNo.Trim();
                drFind["m_caExchangeName"]      = Foreign.m_caExchangeName.Trim();
                drFind["m_caStockNo"]           = Foreign.m_caStockNo.Trim();
                drFind["m_caStockName"]         = Foreign.m_caStockName.Trim();

                drFind["m_nOpen"]               = Foreign.m_nOpen / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nHigh"]               = Foreign.m_nHigh / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nLow"]                = Foreign.m_nLow / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nClose"]              = Foreign.m_nClose / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_dSettlePrice"]        = Foreign.m_dSettlePrice / ( Math.Pow( 10, Foreign.m_sDecimal));

                drFind["m_nTickQty"]            = Foreign.m_nTickQty;
                drFind["m_nRef"]                = Foreign.m_nRef / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nBid"]                = Foreign.m_nBid / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nBc"]                 = Foreign.m_nBc;
                drFind["m_nAsk"]                = Foreign.m_nAsk / ( Math.Pow( 10, Foreign.m_sDecimal));
                drFind["m_nAc"]                 = Foreign.m_nAc;
                drFind["m_nTQty"]               = Foreign.m_nTQty;
            }
        }


        private DataTable CreateBest5AskTable()
        {
            DataTable myDataTable = new DataTable();

            DataColumn myDataColumn;

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Int32");
            myDataColumn.ColumnName = "m_nAskQty";
            myDataTable.Columns.Add(myDataColumn);

            myDataColumn = new DataColumn();
            myDataColumn.DataType = Type.GetType("System.Double");
            myDataColumn.ColumnName = "m_nAsk";
            myDataTable.Columns.Add(myDataColumn);

            return myDataTable;
        }

        private void InsertBest5(BEST5 Best5)
        {
            if (m_dtBest5Ask.Rows.Count == 0 && m_dtBest5Bid.Rows.Count == 0)
            {
                DataRow myDataRow;

                myDataRow = m_dtBest5Ask.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nAskQty1;
                myDataRow["m_nAsk"] = Best5.m_nAsk1;
                m_dtBest5Ask.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Ask.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nAskQty2;
                myDataRow["m_nAsk"] = Best5.m_nAsk2;
                m_dtBest5Ask.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Ask.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nAskQty3;
                myDataRow["m_nAsk"] = Best5.m_nAsk3;
                m_dtBest5Ask.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Ask.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nAskQty4;
                myDataRow["m_nAsk"] = Best5.m_nAsk4;
                m_dtBest5Ask.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Ask.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nAskQty5;
                myDataRow["m_nAsk"] = Best5.m_nAsk5;
                m_dtBest5Ask.Rows.Add(myDataRow);



                myDataRow = m_dtBest5Bid.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nBidQty1;
                myDataRow["m_nAsk"] = Best5.m_nBid1 ;
                m_dtBest5Bid.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Bid.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nBidQty2;
                myDataRow["m_nAsk"] = Best5.m_nBid2;
                m_dtBest5Bid.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Bid.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nBidQty3;
                myDataRow["m_nAsk"] = Best5.m_nBid3;
                m_dtBest5Bid.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Bid.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nBidQty4;
                myDataRow["m_nAsk"] = Best5.m_nBid4;
                m_dtBest5Bid.Rows.Add(myDataRow);

                myDataRow = m_dtBest5Bid.NewRow();
                myDataRow["m_nAskQty"] = Best5.m_nBidQty5;
                myDataRow["m_nAsk"] = Best5.m_nBid5;
                m_dtBest5Bid.Rows.Add(myDataRow);

            }
            else
            {
                m_dtBest5Ask.Rows[0]["m_nAskQty"] = Best5.m_nAskQty1;
                m_dtBest5Ask.Rows[0]["m_nAsk"] = Best5.m_nAsk1;

                m_dtBest5Ask.Rows[1]["m_nAskQty"] = Best5.m_nAskQty2;
                m_dtBest5Ask.Rows[1]["m_nAsk"] = Best5.m_nAsk2;

                m_dtBest5Ask.Rows[2]["m_nAskQty"] = Best5.m_nAskQty3;
                m_dtBest5Ask.Rows[2]["m_nAsk"] = Best5.m_nAsk3;

                m_dtBest5Ask.Rows[3]["m_nAskQty"] = Best5.m_nAskQty4;
                m_dtBest5Ask.Rows[3]["m_nAsk"] = Best5.m_nAsk4;

                m_dtBest5Ask.Rows[4]["m_nAskQty"] = Best5.m_nAskQty5;
                m_dtBest5Ask.Rows[4]["m_nAsk"] = Best5.m_nAsk5;


                m_dtBest5Bid.Rows[0]["m_nAskQty"] = Best5.m_nBidQty1;
                m_dtBest5Bid.Rows[0]["m_nAsk"] = Best5.m_nBid1;

                m_dtBest5Bid.Rows[1]["m_nAskQty"] = Best5.m_nBidQty2;
                m_dtBest5Bid.Rows[1]["m_nAsk"] = Best5.m_nBid2;

                m_dtBest5Bid.Rows[2]["m_nAskQty"] = Best5.m_nBidQty3;
                m_dtBest5Bid.Rows[2]["m_nAsk"] = Best5.m_nBid3;

                m_dtBest5Bid.Rows[3]["m_nAskQty"] = Best5.m_nBidQty4;
                m_dtBest5Bid.Rows[3]["m_nAsk"] = Best5.m_nBid4;

                m_dtBest5Bid.Rows[4]["m_nAskQty"] = Best5.m_nBidQty5;
                m_dtBest5Bid.Rows[4]["m_nAsk"] = Best5.m_nBid5;
            }
        }

        public void readHistoryKLData()
        {
            string ff = Application.StartupPath + @"\HistoryKLine1Min.xml";
            
            using (StreamReader sr = new StreamReader(ff, Encoding.Default, true))
            {
                while (sr.Peek() != -1)
                {
                    string _tkstr = sr.ReadLine();
                    if (_tkstr.IndexOf("JNM") != -1)
                    {
                        if (_tkstr.IndexOf("PM") != -1)
                            OSEJNIPM_ID_CAP = _tkstr;
                        else
                        {
                            OSEJNI_ID_CAP = _tkstr;
                        }
                    }
                }
            }
        }


        #endregion

        private void btnGo_Click(object sender, EventArgs e)
        {
            programStartTime = DateTime.Now;

           
            switch (quoteProgramMode)
            {
                case QuoteProgramModeOS.QPM_AllProduct:
                    {
                        //取得當前商品列表
                        m_nCode = Functions.SKOSQuoteLib_AttachOverseaProductsCallBack(fOverseaProducts);
                        m_nCode = Functions.SKOSQuoteLib_RequestOverseaProducts();
                    }
                    break;
                case QuoteProgramModeOS.QPM_MarketAM://小日經上午盤中作單模式
                    {
                        //計時器，上午盤總時間+15分鐘
                        int remainSec = (int)((g_marketEndTime_AM - programStartTime).TotalSeconds + 900);
                        Console.WriteLine(" 還剩下" + remainSec.ToString() + " 秒關閉");
                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNI_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    break;
                case QuoteProgramModeOS.QPM_AMMarketTickGet:
                    {
                        //小日經上午盤後Tick資訊擷取
                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNI_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    break;
                case QuoteProgramModeOS.QPM_MarketPM:
                    {
                        //大計時器，下午盤總時間+15分鐘
                        int remainSec = (int)((g_marketEndTime_PM - programStartTime).TotalSeconds + 900);
                        
                        Console.WriteLine(" 還剩下" + remainSec.ToString() + " 秒關閉");

                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNIPM_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    break;
                case QuoteProgramModeOS.QPM_AfterMarket:
                    {
                        //小日經下午盤後Tick資訊擷取，寫入每日歷史紀錄
                        int nPage = 0;
                        string strTicks = "OSE," + OSEJNIPM_ID_CAP;
                        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                    }
                    break;
            }
            this.timer2.Interval = 100;
            this.timer2.Start();
        }
    }
}
