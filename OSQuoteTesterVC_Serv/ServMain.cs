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
using System.Threading.Tasks;
using Hik.Communication.Scs.Communication.EndPoints;
using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Service;
using QuoteTester;
using OSCommonLib;
using ZedGraph;

namespace OSQuoteTester
{
    public partial class ServMain : Form
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

        private delegate void InvokeSetLastCBTime(DateTime d);

        // Delegate to collect KLine data and trade.
        private delegate void RequestNotifyTicks(TICK tk);
        private delegate void RequestNotifyBest5(BEST5 _b5);

        private Logger      m_Logger;

        private delegate void InvokeQuoteUpdate(FOREIGN Foreign);

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


        delegate void UpdateControl(Control Ctrl, string Msg);

        private object _objLock = new object();
        void _mUpdateControl(Control Ctrl, string Msg)
        {
            lock (this._objLock)
            {
                if (Ctrl is System.Windows.Forms.Label) // 更新Label文字
                {
                    ((System.Windows.Forms.Label)Ctrl).Text = Msg;
                }
                else if (Ctrl is ListBox)
                {
                    ((ListBox)Ctrl).Items.Add(Msg);
                }
            }
        }

        //Order
        OrderReply.FOnOrderAsyncReport fOrderAsync;
        OrderReply.FOnGetExecutionReoprt fOnExecutionReport;
        OrderReply.FOnOverseaFutureOpenInterest fOnOverseaFutureOpenInterest;
        public delegate void MyMessageHandler(string strType, int nCode, string strMessage);
        public event MyMessageHandler GetMessage;

        //Reply
        OrderReply.FOnData fData;
        OrderReply.FOnConnect fRConnect;
        OrderReply.FOnConnect fDisconnect;
        OrderReply.FOnComplete fComplete;
        bool m_bReplyDisconnect = true;
        private delegate void ConnectReplyServer(string state);

        // Quote Tester 拆分模式
        public bool bServerMode = false;

        private IScsServiceApplication scserv;

        private bool bConnected = false;

        // 嘗試重連
        bool bReconnect = false;

        // 小日經熱門月
        private DateTime OSEHotDate;

        // 小日經熱門月  上午商品字串(群益)
        public static string OSEJNI_ID_CAP;

        // 小日經熱門月  下午商品字串(群益)
        public static string OSEJNIPM_ID_CAP;

        // 小日經熱門月  上午商品字串(凱基)
        public static string OSEJNI_ID_KGI;

        // 小日經熱門月  下午商品字串(凱基)
        public static string OSEJNIPM_ID_KGI;

        //上次callback 收到的時間
        public DateTime LastCbTime = new DateTime();

        //海期所有商品清單
        private List<string> prodStrCol = new List<string>();

        //小日經1分K昨日歷史資料
        private List<KLine> OseJniHistKL1Min = new List<KLine>();

        //小日經1分K昨日歷史資料(文字)
        private List<KLineStr> OseJniHistKL1MinStr = new List<KLineStr>();

        List<string> tickLog = new List<string>();
        public TICK m_Tick;

        // 策略
        JNAutoDayTrade_Strategy dayTradeSt;

        //上午開盤時間，日本時間上午9:00
        public DateTime g_marketStartTime_AM;

        //下午收盤時間，日本時間上午3:00
        public DateTime g_marketEndTime_PM;

        #region ZedGraph 繪圖相關變數

        // K棒
        JapaneseCandleStickItem jpnCandle;

        // Ema11 線
        LineItem ema11LineGr;

        // Ema22 線
        LineItem ema22LineGr;

        // 關心的區段
        public int m_numBarFocused = 45;

        // 時間軸最末端的根數，1代表最末
        private int m_nthBarIsLast = 1;

        // 非同步更新繪圖
        //Func<double, double> updGraphDelegate;
        private delegate void updGraphDelegate( KLine _KL , double a , double b,int min ,int max);
        updGraphDelegate m_updGraphDelegate;

        private delegate string  AsyncEventArgs(string Result);
        private event AsyncEventArgs AsyncEventHandler;

        #endregion

        #endregion

        #region Initialize
        //----------------------------------------------------------------------
        // Initialize
        //----------------------------------------------------------------------
        public ServMain(string[] args)
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
                    Console.WriteLine("當前模式為  Server 一般雙擊開啟");
                    break;
                case 1: quoteProgramMode = QuoteProgramModeOS.QPM_AllProduct;
                    Console.WriteLine("當前模式為  Server 取得當前商品列表");
                    break;
                case 2: quoteProgramMode = QuoteProgramModeOS.QPM_MarketKLGetter;
                    Console.WriteLine("當前模式為  Server 昨日下午盤K棒資訊擷取");
                    break;
                case 3: quoteProgramMode = QuoteProgramModeOS.QPM_MarketAM;
                    Console.WriteLine("當前模式為  Server  小日經上午盤中作單模式");
                    break;
                case 4: quoteProgramMode = QuoteProgramModeOS.QPM_AMMarketTickGet;
                    Console.WriteLine("當前模式為  Server  小日經上午盤後Tick資訊擷取，上午倉位紀錄");
                    break;
                case 5: quoteProgramMode = QuoteProgramModeOS.QPM_MarketPM;
                    Console.WriteLine("當前模式為  Server  小日經下午盤作單模式");
                    break;
                case 6: quoteProgramMode = QuoteProgramModeOS.QPM_AfterMarket;
                    Console.WriteLine("當前模式為  Server  小日經下午盤後Tick資訊擷取，寫入每日歷史紀錄");
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


            // Order Lib
            fOrderAsync = new OrderReply.FOnOrderAsyncReport(OnOrderAsyncReport);
            GC.KeepAlive(fOrderAsync);

            fOnExecutionReport = new OrderReply.FOnGetExecutionReoprt(OnExecutionReport);
            GC.KeepAlive(fOnExecutionReport);
            GC.SuppressFinalize(fOnExecutionReport);


            // Reply Lib
            fData = new OrderReply.FOnData(OnReceiveReplyData);
            GC.KeepAlive(fData);

            fRConnect = new OrderReply.FOnConnect(OnRConnect);
            GC.KeepAlive(fConnect);

            fDisconnect = new OrderReply.FOnConnect(OnRDisconnect);
            GC.KeepAlive(fDisconnect);

            fComplete = new OrderReply.FOnComplete(OnComplete);
            GC.KeepAlive(fComplete);


            m_Logger = new Logger();

            m_Tick = new TICK();

            // 繪圖初始
            StockPointList _stklist = new StockPointList();
            StockPointList _ema11Plist = new StockPointList();
            StockPointList _ema22Plist = new StockPointList();
            
            this.AsyncEventHandler += new AsyncEventArgs(_AsyncEventHandler);
            m_updGraphDelegate = new updGraphDelegate(updGraph);
            CreateGraph(zg1, _stklist, _ema11Plist, _ema22Plist);

            // 啟動策略，更新繪圖
            dayTradeSt = new JNAutoDayTrade_Strategy(Application.StartupPath,this);

            //更新繪圖資訊
            DrawGraphOnce(dayTradeSt.m_KLdata_30Min, dayTradeSt.EMA11.records, dayTradeSt.EMA22.records);
            
            // 取得商品資訊已經搬移到client ，一定是server mode
            //if( 
            //    (quoteProgramMode==QuoteProgramModeOS.QPM_MarketAM) ||
            //    (quoteProgramMode == QuoteProgramModeOS.QPM_AMMarketTickGet) ||
            //    (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM) ||
            //    (quoteProgramMode == QuoteProgramModeOS.QPM_AfterMarket)
            //  )
              bServerMode = true;

            //今天日期
            DateTime todate = DateTime.Now;

            // Server 不分上下午，只需開收盤時間
            g_marketStartTime_AM = new DateTime(todate.Year, todate.Month, todate.Day, 9, 0, 0);
            DateTime nxDate = todate.AddDays(1);
            g_marketEndTime_PM = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 3, 0, 0);

            this.timer2.Interval = 100;
            this.timer2.Start();

            

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

            //如果是拆分模式，就不用開啟QuoteLib
            if (bServerMode)
            {
                #region OrderLib
                // 初始化 Order Lib
                //Initialize SKOrderLib
                m_nCode = OrderReply.Functions.SKOrderLib_Initialize(m_strLoginID, strPassword);

                //if (m_nCode == 0)
                //    MessageBox.Show("Initialize Success");
                if (m_nCode == 2003)
                {
                    MessageBox.Show("元件已初始過，無須重複執行");
                }
                else if (m_nCode != 0)
                {
                    MessageBox.Show("Initialize Fail：code " + GetApiCodeDefine(m_nCode));
                    return;
                }

                //Initialize  Cert
                m_nCode = OrderReply.Functions.SKOrderLib_ReadCertByID(m_strLoginID);
                //if (m_nCode == 0)
                //    MessageBox.Show("ReadCert Success");

                //Get Account
                OrderReply.FOnGetBSTR fAccount = new OrderReply.FOnGetBSTR(OnAccount);
                m_nCode = OrderReply.Functions.RegisterOnAccountCallBack(fAccount);
                m_nCode = OrderReply.Functions.GetUserAccount();

                //OrderAsync CallBack
                m_nCode = OrderReply.Functions.RegisterOnOrderAsyncReportCallBack(fOrderAsync);

                m_nCode = OrderReply.Functions.RegisterOnExecutionReportCallBack(fOnExecutionReport);
                #endregion
                #region ReplyLib
                //Initialize SKOrderLib
                m_nCode = OrderReply.Functions.SKReplyLib_Initialize(m_strLoginID, strPassword);

                //if (m_nCode == 0)
                // MessageBox.Show("SKReplyLib_Initialize Success");
                if (m_nCode == 2003)
                {
                    MessageBox.Show("元件已初始過，無須重複執行");
                }
                else if (m_nCode != 0)
                {
                    MessageBox.Show("SKReplyLib_Initialize  Fail：code " + GetApiCodeDefine(m_nCode));
                    return;
                }

                //OnConnect CallBack
                m_nCode = OrderReply.Functions.RegisterOnConnectCallBack(fRConnect);

                //OnDisconnect CallBack
                m_nCode = OrderReply.Functions.RegisterOnDisconnectCallBack(fDisconnect);

                //OnData CallBack
                m_nCode = OrderReply.Functions.RegisterOnDataCallBack(fData);

                //OnComplete CallBack
                m_nCode = OrderReply.Functions.RegisterOnCompleteCallBack(fComplete);

                #endregion
                scserv = ScsServiceBuilder.CreateService(new ScsTcpEndPoint(10083));
                scserv.AddService<IOSQuoteService, JNAutoDayTrade_Strategy>(dayTradeSt);
                scserv.Start();
                
            }
            else
            {
                #region QuoteLib
                //Initialize SKOrderLib
                m_nCode = Functions.SKOSQuoteLib_Initialize(m_strLoginID, strPassword);

                //m_Logger.Write("SKOSQuoteLib_Initialize  Code：" + m_nCode.ToString());

                if (m_nCode == 0)
                {
                    Console.WriteLine("Initialize Success");
                    //lblMessage.Text = "元件初始化完成";
                    //btnConnect_Click(null, null);
                }
                else if (m_nCode == 2003)
                {
                    //lblMessage.Text = "元件已初始過，無須重複執行";
                }
                else
                {
                    //lblMessage.Text = "元件初始化失敗 code " + GetApiCodeDefine(m_nCode);
                    return;
                }

                m_nCode = Functions.SKOSQuoteLib_AttachConnectCallBack(fConnect);

                m_nCode = Functions.SKOSQuoteLib_AttachQuoteCallBack(fQuoteUpdate);

                m_nCode = Functions.SKOSQuoteLib_AttachTicksCallBack(fNotifyTicks);

                m_nCode = Functions.SKOSQuoteLib_AttachBest5CallBack(fOnNotifyBest5);

                m_nCode = Functions.SKOSQuoteLib_AttachServerTimeCallBack(fNotifyServerTime);

                m_nCode = Functions.SKOSQuoteLib_AttachHistoryTicksGetCallBack(fOnNotifyTicksGet);

                return;
                #endregion
            }

            if (m_nCode == 1)
            {
                //MessageBox.Show("Initialize Success");
                //lblMessage.Text = "元件初始化完成";
                btnConnect_Click(sender, e);

                this.timer2.Enabled = true;
                this.timer2.Interval = 100;
                this.timer2.Start();
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (bServerMode)
            {
                m_nCode = OrderReply.Functions.SKReplyLib_IsConnectedByID(m_strLoginID);

                if (m_nCode == 0)
                {
                    OrderReply.Functions.SKReplyLib_ConnectByID(m_strLoginID);
                }
            }
            //else
            //{
            //    m_nCode = Functions.SKOSQuoteLib_EnterMonitor(0);
            //    if (m_nCode != 0)
            //    {
            //        //lblMessage.Text = "連線失敗 code " + GetApiCodeDefine(m_nCode);
            //        return;
            //    }
            //}
            //m_Logger.Write("SKOSQuoteLib_EnterMonitor Code:" + m_nCode.ToString());
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            m_nCode = Functions.SKOSQuoteLib_LeaveMonitor();

            if (m_nCode != 0)
            {
                //lblMessage.Text = "斷線失敗 code " + GetApiCodeDefine(m_nCode);
            }
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
               // lblMessage.Text = "查詢商品失敗 code " + GetApiCodeDefine(m_nCode);
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
                //lblMessage.Text = "查詢Ticks失敗 code " + GetApiCodeDefine(m_nCode);
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
               // lblMessage.Text = "查詢時間失敗 code " + GetApiCodeDefine(m_nCode);
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
                bConnected = true;
            }
            //else if (nKind == 101)
            //{
            //    WriteInfo("Disconnect Code: " + GetApiCodeDefine(nErrorCode));
            //    lblConnect.ForeColor = Color.Red;
            //    bConnected = false;
            //    //重連
            //    btnConnect_Click(null, null);
            //}
        }
        public void OnAccount(string bstrData)
        {
            string[] strValues;
            string strAccount;

            strValues = bstrData.Split(',');
            strAccount = strValues[1] + strValues[3];

            if (strValues[0] == "OF")
            {
                dayTradeSt.futureAccount = strAccount;
            }
        }

        public void OnQuoteUpdate(short nStockIdx)
        {
            FOREIGN Foreign;

            m_nCode = Functions.SKOSQuoteLib_GetStockByIndex(nStockIdx, out Foreign);

            if (m_nCode != 0)
            {
               // lblMessage.Text = "商品報價查詢失敗 " + GetApiCodeDefine(m_nCode);
            }
            else
            {
                this.Invoke(new InvokeQuoteUpdate(this.OnUpDateDataQuote), new object[] { Foreign });
            }
        }

        public void OnNotifyTicks( short sStockIdx , int nPtr )
        {
            //m_Logger.Write("OnQuoteUpdate CallBack 通知 sStockIdx:" + sStockIdx.ToString() + " nPtr:" + nPtr.ToString());
            
            //if (listTicks.InvokeRequired == true)
            //{
            //    this.Invoke(new InvokeTick(this.OnNotifyTicks), new object[] { sStockIdx, nPtr });
            //}
            //else
            {
                TICK tTick;
                m_nCode = Functions.SKQuoteLib_GetTick(sStockIdx, nPtr, out tTick);

                if (m_nCode == 0)
                {
                    listTicks.Items.Add("時間：" + tTick.m_nTime.ToString() + "  成交價：" + tTick.m_nClose.ToString() + "  量：" + tTick.m_nQty.ToString());
                    listTicks.SelectedIndex = listTicks.Items.Count - 1;
                }
                Console.WriteLine("時間：" + tTick.m_nTime.ToString() + "  成交價：" + tTick.m_nClose.ToString() + "  量：" + tTick.m_nQty.ToString());
                if ((quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) || (quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
                {
                    this.Invoke(new RequestNotifyTicks(dayTradeSt.OnNotifyTicks), new object[] { tTick });
                    //LastCbTime = DateTime.Now;
                    this.Invoke(new InvokeSetLastCBTime(SetLastCBTime), new object[] { DateTime.Now });
                }
            }
        }

        public void OnNotifyTicksGet( short sStockidx,int nPtr, int nTime, int nClose, int nQty)
        {
            //Console.WriteLine(nTime);
            if (listTicks.InvokeRequired == true)
            {
                this.Invoke(new InvokeHisTick(this.OnNotifyTicksGet), new object[] {  sStockidx,nPtr, nTime, nClose, nQty });
            }
            else
            {
                //string strMsg = "時間：" + nTime.ToString() + "  成交價：" + nClose.ToString() + "  量：" + nQty.ToString();
                //listTicks.Items.Add(strMsg);
                //listTicks.SelectedIndex = listTicks.Items.Count - 1;

                if ((quoteProgramMode == QuoteProgramModeOS.QPM_AMMarketTickGet) || (quoteProgramMode == QuoteProgramModeOS.QPM_AfterMarket))
                {
                    string tkStr = "<Tick Time:" + nTime.ToString() + "= Price:" + nClose.ToString() + " Amount:" + nQty.ToString() + " \" />";
                    tickLog.Add(tkStr);
                    LastCbTime = DateTime.Now;
                }
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
                    this.Invoke(new RequestNotifyBest5(dayTradeSt.OnNotifyBest5), new object[] { best5 });
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
            //Console.WriteLine(sHour.ToString() + "：" + sMinute.ToString() + "：" + sSecond.ToString());
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

        public void OnReceiveReplyData(IntPtr bstrData)
        {
            IntPtr bstrmyData = bstrData;
            OrderReply.DataItem myDataItem = (OrderReply.DataItem)Marshal.PtrToStructure(bstrmyData, typeof(OrderReply.DataItem));

            String m_strData = string.Format("委託序號：{0}   市場類別：{1}   委託種類：{2}    錯誤：{3}   .........營業員代碼：{4}   來源：{5}", myDataItem.strKeyNo, myDataItem.strMarketType, myDataItem.strType, myDataItem.strOrderErr, myDataItem.strSaleNo, myDataItem.strAgent);
            //WriteInfo(m_strData);
            //this.Invoke(new InvokeSendMessage(this.WriteInfo), new object[] { m_strData });

            string fileSavePath = Application.StartupPath + @"\replydata.log";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileSavePath, false, System.Text.Encoding.ASCII))
            {
                file.WriteLine(m_strData);  
            }

            Marshal.FreeBSTR(bstrmyData);

            dayTradeSt.OnReply(myDataItem);
            return;
            
        }
        //OnComplete CallBack Function
        public void OnComplete(int nCode)
        {
            string strInfo = "Server連線確認      Code: " + GetApiCodeDefine(nCode);

            //WriteInfo(strInfo);
            //Console.WriteLine(strInfo);
        }

        public void OnOrderAsyncReport(int nThreadID, int nCode, string strMessage)
        {
            return;
        }
        public void OnExecutionReport(string strData)
        {
            //this.Invoke(new InvokeSendMessage(InsertData), new object[] { strData });
            return;
        }
        public void OnRConnect(string strID, int nErrorCode)
        {
            string strInfo = "回報連線 " + strID + "        Code: " + GetApiCodeDefine(nErrorCode);
            Console.WriteLine(strInfo);
            bConnected = true;

            if (nErrorCode == 0)
            {
                lblConnect.ForeColor = Color.Green;

                //取得未平倉位
                if (quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM)
                {
                    fOnOverseaFutureOpenInterest = new OrderReply.FOnOverseaFutureOpenInterest(OnOverseaFutureOpenInterest);
                    m_nCode = OrderReply.Functions.RegisterOnOverseaFutureOpenInterestCallBack(fOnOverseaFutureOpenInterest);
                    m_nCode = OrderReply.Functions.GetOverseaFutureOpenInterest(dayTradeSt.futureAccount);
                }
                if(bReconnect)
                    Console.WriteLine("+++++++++ Reconnected +++++++++ Time: " + DateTime.Now.ToString());
                bReconnect = false;
            }
        }
        //Disconnect CallBack Function
        public void OnRDisconnect(string strID, int nErrorCode)
        {
            //string strInfo = "回報斷線 " + strID + "       Code: " + GetApiCodeDefine(nErrorCode) + " 一分後重新連線 ";
            //Console.WriteLine(strInfo);
            //this.Invoke(new ConnectReplyServer(this.ReConnectReplyServer), new object[] { strInfo });
            lblConnect.ForeColor = Color.Red;


            // Try to reconnect
            Console.WriteLine("+++++++++ Disconnected +++++++++ Time: " + DateTime.Now.ToString());
            if ((DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketStartTime_AM)) > 0) &&
                      (DateTime.Compare(DateTime.Now, convertJpnTimeToTw(g_marketEndTime_PM)) < 0))
            {
                bReconnect = true;
                btnConnect_Click(null, null);
            }
        }
        public void ReConnectReplyServer(string strMsg)
        {
            bReconnect = true;
        }
        private void SetLastCBTime(DateTime d)
        {
            LastCbTime = d;
        }

        // 取得未平倉部位資訊
        void OnOverseaFutureOpenInterest(string strData)
        {
            //"OSE,大阪證券交易所,F0200001709003,JNM    201503,mini大阪日經 201503,B,1,18835.00000,18815.0000,18630.00000,2000.00"
           // string strtest = "OSE,大阪證券交易所,F0200001709003,JNM    201503,mini大阪日經 201503,B,1,18835.00000,18815.0000,18630.00000,2000.00";
            if (JNAutoDayTrade_Strategy.ContainStr(strData, "OSE,"))
            {
                string[] stspl = strData.Split(',');
                bool btradePositive = stspl[5].Equals("B");
                int invCnt = int.Parse(stspl[6]);
                int lastDealPrice = int.Parse(stspl[8].Split('.')[0]);

                dayTradeSt.g_LastDealPrice = lastDealPrice;
                dayTradeSt.g_CurDeposit = invCnt;
                dayTradeSt.g_b1MinTradePositive = btradePositive;
                // 建立倉位價格配對表
                for (int k = 0; k < invCnt; k++)
                {
                    TradeInOutPrice inout = new TradeInOutPrice();
                    inout.In = lastDealPrice;
                    inout.Out = 0;
                    dayTradeSt.inOutPriceList.Add(inout);
                }
            }
        }


        #endregion

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
            //if (lblMessage.InvokeRequired == true)
            //{
            //    this.Invoke(new InvokeSendMessage(this.WriteInfo), new object[] { strMsg });
            //}
            //else
            //{
            //    lblMessage.Text = strMsg;
            //}
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

        public static void exit()
        {
            Process.GetCurrentProcess().Kill();
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

        private void saveLog_AM()
        {
            string fileSavePath = Application.StartupPath + @"\AMTicks.xml";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileSavePath, false, System.Text.Encoding.ASCII))
            {
                //寫入上午Tick
                foreach (string line in tickLog)
                {
                    file.WriteLine(line);
                }
            }
        }
        
        private void saveLog_PM()
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

        #endregion


        private void timer1_Tick(object sender, EventArgs e)
        {
            //Console.WriteLine(dayTradeSt.convertJpnTimeToTW(dayTradeSt.g_marketEndTime_AM).ToString());
            m_nCode = Functions.SKOSQuoteLib_RequestServerTime();

            
            switch(quoteProgramMode)
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
                    int days = 150;
                    foreach (DateTime da in prodOSEHot3)
                    {
                        int nd = (int)(da - _todate).TotalDays;
                        if (nd < days)
                        {
                            OSEHotDate = da;
                            days = nd;
                        }
                    }
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
                        m_nCode = Functions.SKOSQuoteLib_RequestKLine("OSE,"+ OSEJNIPM_ID_CAP, (short)0);
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
                //case (QuoteProgramModeOS.QPM_MarketAM):
                //{
                //    if (DateTime.Compare(DateTime.Now, dayTradeSt.convertJpnTimeToTw(dayTradeSt.g_marketEndTime_AM)) > 0)
                //    { 
                //        btnDisconnect_Click(null,null);
                //        if (bServerMode && bConnected)
                //        {
                //            scserv.Stop();
                //            // close reply
                //            m_bReplyDisconnect = true;
                //            m_nCode = OrderReply.Functions.SKReplyLib_CloseByID(m_strLoginID);
                //        }
                //        Process.GetCurrentProcess().Kill();
                //    }   
                //}
                //break;
                #endregion
                #region 下午盤中
                //case (QuoteProgramModeOS.QPM_MarketPM):
                //{
                //    if (DateTime.Compare(DateTime.Now, dayTradeSt.convertJpnTimeToTw(dayTradeSt.g_marketEndTime_PM)) > 0)
                //    {
                //        if (dayTradeSt.g_CurDeposit == 0)
                //        {
                //            btnDisconnect_Click(null, null);
                //            if (bServerMode && bConnected)
                //            {
                //                scserv.Stop();
                //                // close reply
                //                m_bReplyDisconnect = true;
                //                m_nCode = OrderReply.Functions.SKReplyLib_CloseByID(m_strLoginID);
                //            }
                //            Process.GetCurrentProcess().Kill();
                //        }
                //    }
                //}
                //break;
                #endregion
                #region QPM_AMMarketTickGet
                //case (QuoteProgramModeOS.QPM_AMMarketTickGet):
                //{
                //    if (LastCbTime.Year < 2) return;//至少有一次callback
                //    if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                //    saveLog_AM();
                //    if (bServerMode)
                //    {
                //        scserv.Stop();
                //        // close reply
                //        m_bReplyDisconnect = true;
                //        m_nCode = OrderReply.Functions.SKReplyLib_CloseByID(m_strLoginID);
                //    }
                //    Process.GetCurrentProcess().Kill();
                //}
                //break;
                #endregion
                #region QPM_AfterMarket
                //case (QuoteProgramModeOS.QPM_AfterMarket):
                //{
                //    if (LastCbTime.Year < 2) return;//至少有一次callback
                //    if ((DateTime.Now - LastCbTime).TotalSeconds < 5) return;
                //    if (bServerMode)
                //    {
                //        scserv.Stop();
                //        // close reply
                //        m_bReplyDisconnect = true;
                //        m_nCode = OrderReply.Functions.SKReplyLib_CloseByID(m_strLoginID);
                //    }
                //    saveLog_PM();
                //    Process.GetCurrentProcess().Kill();
                //}
                //break;
                #endregion
            }
        }
        private void timer2_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            string msg = LastCbTime.Hour.ToString() + ":" + LastCbTime.Minute.ToString() + ":" + LastCbTime.Second.ToString();
            this.BeginInvoke(new UpdateControl(_mUpdateControl), new object[] { this.lblServerTime, msg });

            if (DateTime.Compare(DateTime.Now, dayTradeSt.convertJpnTimeToTw(dayTradeSt.g_marketEndTime_PM)) > 0)
            {
                if (dayTradeSt.g_CurDeposit == 0)
                {
                    btnDisconnect_Click(null, null);
                    if (bServerMode && bConnected)
                    {
                        scserv.Stop();
                        // close reply
                        m_bReplyDisconnect = true;
                        m_nCode = OrderReply.Functions.SKReplyLib_CloseByID(m_strLoginID);
                    }
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

       

        private void btnGo_Click(object sender, EventArgs e)
        {
            switch (quoteProgramMode)
            {
                case QuoteProgramModeOS.QPM_AllProduct:
                    {
                        //取得當前商品列表
                        m_nCode = Functions.SKOSQuoteLib_AttachOverseaProductsCallBack(fOverseaProducts);
                        m_nCode = Functions.SKOSQuoteLib_RequestOverseaProducts();
                        this.timer1.Enabled = true;
                        this.timer1.Interval = 15000;
                    }
                    break;
                #region DEPRECATED
                //case QuoteProgramModeOS.QPM_MarketAM:
                //    {
                //        //小日經上午盤中作單模式
                //        int nPage = 0;
                //        string strTicks = "OSE," + OSEJNI_ID_CAP;
                //        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                //    }
                //    break;
                //case QuoteProgramModeOS.QPM_AMMarketTickGet:
                //    {
                //        //小日經上午盤後Tick資訊擷取
                //        int nPage = 0;
                //        string strTicks = "OSE," + OSEJNI_ID_CAP;
                //        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                //    }
                //    break;
                //case QuoteProgramModeOS.QPM_MarketPM:
                //    {
                //        fOnOverseaFutureOpenInterest = new OrderReply.FOnOverseaFutureOpenInterest(OnOverseaFutureOpenInterest);
                //        m_nCode = OrderReply.Functions.RegisterOnOverseaFutureOpenInterestCallBack(fOnOverseaFutureOpenInterest);
                //        m_nCode = OrderReply.Functions.GetOverseaFutureOpenInterest(dayTradeSt.futureAccount); // ; m_strLoginID

                //    }
                //    break;
                //case QuoteProgramModeOS.QPM_AfterMarket:
                //     {
                //         //小日經下午盤後Tick資訊擷取，寫入每日歷史紀錄
                //        int nPage = 0;
                //        string strTicks = "OSE," + OSEJNIPM_ID_CAP;
                //        m_nCode = Functions.SKOSQuoteLib_RequestTicks(out nPage, strTicks);
                //    }
                //    break;
                #endregion
            }
        }



        private void btnTestHistory_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int totalWinPnt = 0;
                int totalTradTimes = 0;
                string ff = openFileDialog1.FileName;

                //Force GC
                System.GC.Collect();
                GC.WaitForPendingFinalizers();
                JNMTradeSim histTradeSim = new JNMTradeSim(openFileDialog1.FileName);              
            }

        }

        private void bnTestBuy_Click(object sender, EventArgs e)
        {
            int nCode = -1;
            int nNewClose = 2;//自動
            string _price="0";
            string strStockNo = "JNM";
            //if (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
            //    strStockNo = "JNMPM";
            int nSpecialTradeType = 0;//限價
            int iBuySell = 0 ;
            int nDayTrade = 1; // 當沖
            int nTradeType = 4; //FOK
            string strProdYM = "20" + ServMain.OSEJNI_ID_CAP.Substring(ServMain.OSEJNI_ID_CAP.IndexOf("JNM") + 3, 4);
            int nQty = 1;
            {
                nQty = (dayTradeSt.m_best5.m_nAskQty1 >= 1) ? 1 : dayTradeSt.m_best5.m_nAskQty1;
                _price = (dayTradeSt.m_best5.m_nAsk1).ToString();
            }

            if (nQty > 0)
            {   
                StringBuilder strMessage = new StringBuilder();
                strMessage.Length = 1024;
                Int32 nSiz = 1024;

                nCode = OrderReply.Functions.SendOverseaFutureOrderAsync(dayTradeSt.futureAccount, "OSE", strStockNo, strProdYM, iBuySell, nNewClose, nDayTrade, nTradeType, nSpecialTradeType, nQty, _price, "0", "0", "0");
            }
        }

        private void bnTestSell_Click(object sender, EventArgs e)
        {
            int nCode = -1;
            int nNewClose = 2;//自動
            string _price;
            string strStockNo = "JNM";
            //if (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
            //    strStockNo = "JNMPM";
            int nSpecialTradeType = 0;//限價
            int iBuySell = 1;
            int nDayTrade = 1; // 當沖
            int nTradeType = 4; //FOK
            string strProdYM = "20"+ ServMain.OSEJNI_ID_CAP.Substring(ServMain.OSEJNI_ID_CAP.IndexOf("JNM") + 3, 4);
            int nQty = 1;
            {
                nQty = (dayTradeSt.m_best5.m_nBidQty1 >= 1) ? 1 : dayTradeSt.m_best5.m_nBidQty1;
                _price = (dayTradeSt.m_best5.m_nBid1).ToString();
            }

            if (nQty > 0)
            {
                StringBuilder strMessage = new StringBuilder();
                strMessage.Length = 1024;
                Int32 nSiz = 1024;

                nCode = OrderReply.Functions.SendOverseaFutureOrderAsync(dayTradeSt.futureAccount, "OSE", strStockNo, strProdYM, iBuySell, nNewClose, nDayTrade, nTradeType, nSpecialTradeType, nQty, _price, "0", "0", "0");
            }
        }
        #region ZedGraph 繪圖相關函式
        public void CreateGraph( ZedGraphControl zgc, StockPointList stlist,
            StockPointList ema11Plist,StockPointList ema22Plist)
        {
            GraphPane myPane = zgc.GraphPane;

            #region 初始化
            myPane.Title.Text = "小日經 30分鐘";
            myPane.XAxis.Title.Text = "";
            myPane.YAxis.Title.Text = "";
            myPane.XAxis.Type = AxisType.Date;
            myPane.XAxis.Scale.Format = "MM/dd HH:mm";
            myPane.XAxis.Scale.FontSpec.Size = 12;
            myPane.XAxis.Scale.MajorUnit = DateUnit.Minute;
            myPane.XAxis.Scale.MajorStep = 30;
            myPane.XAxis.Scale.MinorUnit = DateUnit.Minute;
            myPane.XAxis.Scale.MinorStep = 10;

            myPane.YAxis.Scale.MagAuto = false;
            myPane.CurveList.numBarsToFocus = m_numBarFocused;

            #endregion

            #region K 棒繪製 : Once
            jpnCandle = myPane.AddJapaneseCandleStick("", stlist);
            jpnCandle.Stick.IsAutoSize = true;

            // 下跌K
            jpnCandle.Stick.FallingFill = new Fill(Color.Green);
            jpnCandle.Stick.FallingColor = Color.Green;
            jpnCandle.Stick.FallingBorder = new Border(false, Color.Green, 0);

            // 上漲K
            jpnCandle.Stick.RisingFill = new Fill(Color.Red);
            jpnCandle.Stick.Color = Color.Red;
            jpnCandle.Stick.RisingBorder = new Border(false, Color.Green, 0);

            #endregion

            #region 背景 : Once

            myPane.Chart.Fill = new Fill(Color.Black); //  Color.LightGoldenrodYellow, 45F 
            myPane.Fill = new Fill(Color.White); //, Color.FromArgb( 0, 0, 255 ), 45F
            myPane.XAxis.Type = AxisType.DateAsOrdinal;
            myPane.Chart.Fill = new Fill(Color.Black);

            #endregion

            #region Range 更新 : Once

            //左右Range
            XDate Min = new XDate(2015, 2, 25, 0, 0, 0, 0);
            XDate Max = new XDate(2015, 2, 26, 0, 0, 0, 0);
            myPane.XAxis.Scale.Min = Min;
            myPane.XAxis.Scale.Max = Max;

            //上下Range
            int min_rr = 16000;
            int max_rr = 19000;
            myPane.YAxis.Scale.Min = min_rr;
            myPane.YAxis.Scale.Max = max_rr;

            #endregion

            zgc.AxisChange();
            zgc.Invalidate();
        }

        // 讀取完歷史資料一次畫出歷史
        public void DrawGraphOnce( List<KLine> _klLst,
            List<double> ema11Lst, List<double> ema22Lst)
        {
            GraphPane myPane = zg1.GraphPane;

            StockPointList _ema11Plist = new StockPointList();
            StockPointList _ema22Plist = new StockPointList();

            List<StockPt> KLPnts = (List<StockPt>)jpnCandle.Points;
            KLPnts.Clear();
            foreach (KLine _kl in _klLst)
            {
                XDate xDate = new XDate(_kl.datetime.Year, _kl.datetime.Month, _kl.datetime.Day, _kl.datetime.Hour, _kl.datetime.Minute, 0);
                StockPt pt_kl = new StockPt(xDate.XLDate, _kl.highest, _kl.lowest, _kl.open, _kl.close, _kl.amount);
                KLPnts.Add(pt_kl);
            }
            
            // Ema 11 更新
            _ema11Plist.Clear();
            for (int k = 0; k < _klLst.Count; k++)
            {
                KLine _kl = _klLst[k];
                double _ema11val = ema11Lst[k];
                XDate xDate = new XDate(_kl.datetime.Year, _kl.datetime.Month, _kl.datetime.Day, _kl.datetime.Hour, _kl.datetime.Minute, 0);
                StockPt pt_ema11 = new StockPt(xDate.XLDate, _ema11val, _ema11val, _ema11val, _ema11val, _ema11val);
                _ema11Plist.Add(pt_ema11);
            }
            
            // Ema 22 更新
            _ema22Plist.Clear();
            for (int k = 0; k < _klLst.Count; k++)
            {
                KLine _kl = _klLst[k];
                double _ema22val = ema22Lst[k];
                XDate xDate = new XDate(_kl.datetime.Year, _kl.datetime.Month, _kl.datetime.Day, _kl.datetime.Hour, _kl.datetime.Minute, 0);
                StockPt pt_ema11 = new StockPt(xDate.XLDate, _ema22val, _ema22val, _ema22val, _ema22val, _ema22val);
                _ema22Plist.Add(pt_ema11);
            }

            ema11LineGr = myPane.AddCurve("ema11", _ema11Plist, Color.Aqua, SymbolType.None);
            ema22LineGr = myPane.AddCurve("ema22", _ema22Plist, Color.Magenta, SymbolType.None);


            edtNumFocus_TextChanged(null, null);
        }

        public IAsyncResult BeginUpdGraph( KLine _KL, double newEma11, double newEma22,int min,int max )
        {
            return m_updGraphDelegate.BeginInvoke(_KL, newEma11, newEma22, min, max, EndUpdGraph, m_updGraphDelegate);
        }

        public void EndUpdGraph(IAsyncResult asyncResult)
        {
            updGraphDelegate del = asyncResult.AsyncState as updGraphDelegate;
            del.EndInvoke(asyncResult);
            if (AsyncEventHandler != null)
                AsyncEventHandler("Draw finished!");
        }
        private string _AsyncEventHandler(string res)
        {
            //Console.WriteLine(res);
            return res;
        }

        public void updGraph(KLine newKL, double newEma11, double newEma22 , int min_rr , int max_rr )
        {
            
            GraphPane myPane = this.zg1.GraphPane;

            //Console.WriteLine("KL: [op:" + newKL.open.ToString() + "] [cl:" + newKL.close.ToString() + "]");
            // K 棒更新
            //KLine newKL = KLLst[KLLst.Count - 2];
            List<StockPt> KLPnts = (List<StockPt>)jpnCandle.Points;
            XDate xDate = new XDate(newKL.datetime.Year, newKL.datetime.Month, newKL.datetime.Day, newKL.datetime.Hour, newKL.datetime.Minute, 0);
            StockPt pt_kl = new StockPt(xDate.XLDate, newKL.highest, newKL.lowest, newKL.open, newKL.close, newKL.amount);
            KLPnts.Add(pt_kl);

            // Ema 11 更新
            List<StockPt> Ema11Pnts = (List<StockPt>)ema11LineGr.Points;
            StockPt pt_ema11 = new StockPt(xDate.XLDate, newEma11, newEma11, newEma11, newEma11, newEma11);
            Ema11Pnts.Add(pt_ema11);

            // Ema 11 更新
            List<StockPt> Ema22Pnts = (List<StockPt>)ema22LineGr.Points;
            StockPt pt_ema22 = new StockPt(xDate.XLDate, newEma22, newEma22, newEma22, newEma22, newEma22);
            Ema22Pnts.Add(pt_ema22);


            #region Range 更新 : Update

            
            
            
            myPane.YAxis.Scale.Min = min_rr;
            myPane.YAxis.Scale.Max = max_rr;
            //Console.WriteLine(min_rr.ToString());

            #endregion

            zg1.AxisChange();
            zg1.Invalidate();
        }
        #endregion

        #region ZedGraph 繪圖事件
        private void edtNumFocus_TextChanged(object sender, EventArgs e)
        {
            TextBox sd = ((TextBox)sender);
            m_numBarFocused = 45;
            if (sd != null)
            {
                if (sd.Text.Length > 0)
                    m_numBarFocused = int.Parse(sd.Text);
            }
            GraphPane myPane = zg1.GraphPane;
            myPane.CurveList.numBarsToFocus = m_numBarFocused;


            //上下Range n 根高低點 + 10%
            int min_rr = 9999999;
            int max_rr = -9999999;

            if (dayTradeSt.m_KLdata_30Min.Count >= m_numBarFocused)
            {
                for (int k = 1; k < (m_numBarFocused + 1); k++)
                {
                    int cLastBar = this.m_nthBarIsLast + k;
                    KLine curKL = dayTradeSt.m_KLdata_30Min[dayTradeSt.m_KLdata_30Min.Count - cLastBar];
                    if (curKL.highest > max_rr)
                        max_rr = curKL.highest;
                    if (curKL.lowest < min_rr)
                        min_rr = curKL.lowest;
                }

                int rng = Math.Abs(max_rr - min_rr);
                double rng10perc = rng * 0.1;
                max_rr += (int)rng10perc;
                min_rr -= (int)rng10perc;
            }
            else
            {
                min_rr = 16000;
                max_rr = 19000;
            }
            myPane.YAxis.Scale.Min = min_rr;
            myPane.YAxis.Scale.Max = max_rr;

            zg1.AxisChange();
            zg1.Invalidate();
        }

        private void btnGrpToLeft_Click(object sender, EventArgs e)
        {
            List<StockPt> KLPnts = (List<StockPt>)jpnCandle.Points;
            int numBarToJump = 1;
            if (this.edtGrpStep != null)
            {
                if (this.edtGrpStep.Text.Length > 0)
                {
                    try
                    {
                        numBarToJump = int.Parse(this.edtGrpStep.Text);
                    }
                    catch { }
                }
            }
            this.m_nthBarIsLast += numBarToJump;
            if((KLPnts.Count - this.m_nthBarIsLast)<=m_numBarFocused )
                this.m_nthBarIsLast = KLPnts.Count-m_numBarFocused;

            GraphPane myPane = this.zg1.GraphPane;
            myPane.CurveList.numBarsToEnd = this.m_nthBarIsLast;
            edtNumFocus_TextChanged(null, null);
        }

        private void btnGrpToRight_Click(object sender, EventArgs e)
        {
            List<StockPt> KLPnts = (List<StockPt>)jpnCandle.Points;
            int numBarToJump = 1;
            if (this.edtGrpStep != null)
            {
                if (this.edtGrpStep.Text.Length > 0)
                {
                    try
                    {
                        numBarToJump = int.Parse(this.edtGrpStep.Text);
                    }
                    catch { }
                }
            }
            this.m_nthBarIsLast -= numBarToJump;
            if (this.m_nthBarIsLast < 0)
                this.m_nthBarIsLast = 0;

            GraphPane myPane = this.zg1.GraphPane;
            myPane.CurveList.numBarsToEnd = this.m_nthBarIsLast;
            edtNumFocus_TextChanged(null, null);
        }

        #endregion
    }
}
