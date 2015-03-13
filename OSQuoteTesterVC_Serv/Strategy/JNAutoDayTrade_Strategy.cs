using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;
using OrderReply;
using _4XLab.NET;
using OSQuoteTester;
using OSCommonLib;
using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using Hik.Communication.ScsServices.Service;
using System.ComponentModel;

/*
 *          小日經日內當沖程式 v1.
 *          0 版
 * 
 */

namespace QuoteTester
{
    // 等待成交回報模式
    enum waitDealMode
    {
        notWaiting,
        waitToBuildInventory,
        waitToCleanInventory,
    };

    // 做單模式，OrderTest 可以訪問
    enum operationMode
    {
        eAutrMode_FakeTrader,		//貼近真實盤面的模擬下單
        eAutrMode_DuringMarket,		//盤中做單
        eAutrMode_TestHistory,		//讀取歷史紀錄模擬做單
    };

    // 平倉模式
    enum cleanupMode
    {
        cleanupMode_KLEnd,			//行情出現反轉平倉
        cleanupMode_DayEnd,			//收盤前平倉
    };

    //送單買賣代號
    // 0:買進 1:賣出
    enum tBuySell
    {
        Buy,
        Sell,
    };

    public class TradeInOutPrice
    {
        public int In;
        public int Out;
    }

  
    class JNAutoDayTrade_Strategy : ScsService, IOSQuoteService
    {
        #region Parameters
        public string username = "";
        public string password = "";
        public string futureAccount = "";
        public string prodName = "";

        public DateTime todate = DateTime.Now;

        //當前最近Tick時間
        public DateTime g_CurTime = DateTime.Now;

        // 與server對時的時間
        public DateTime g_ServerTime = DateTime.Now;

        // 今日 K 棒
        public KLine KLineToday = new KLine(86400, DateTime.Now, 0, -99999999, 99999999, 0, 0);

        //上午開盤時間，日本時間上午9:00
        public DateTime g_marketStartTime_AM;

        //上午收盤時間，日本時間下午3:15
        public DateTime g_marketEndTime_AM;

        //下午開盤時間，日本時間下午2:30
        public DateTime g_marketStartTime_PM;

        //下午收盤時間，日本時間上午3:00
        public DateTime g_marketEndTime_PM;

        //收盤強制平倉時間，日本時間上午2:40
        DateTime g_cleanIntrestTime = DateTime.Now;

        //上次出手時間
        DateTime g_LastTradeTime;

        // 上次進出場的時間
        DateTime g_lastOrderSentTime = DateTime.Now;

        // 時間超出24點
        bool bTimeOver24 = false;

        // 可用口數，1口進場，1口加碼
        int g_AvailableDeposit = 2;

        // 當前倉口數
        public int g_CurDeposit = 0;

        // 每次進場使用口數
        int numContractPerTrade = 1;

        // 加碼口數
        int numAddOnPerTime = 1;

        // 真正盤中報價口數
        int nQty = 0;

        // 做單方向，預設多方
        public bool g_b1MinTradePositive = true;

        // 最大停損，單位：點
        int g_MaxStopLoss = 300;

        // 是否剛送出委託，是否等待回報中
        bool bOrderSent = false;

        // 上次進場價位，會被移動
        int g_LastPrice = 0;

        public bool bDoTrading = false;

        //歷史K線數量
        public int historyKLCnt = 0;

        private int ONE_MINUTE = 60;

        private int TEN_MINUTE = 600;

        private int THIRTY_MINUTE = 1800;

        public List<TradeInOutPrice> inOutPriceList = new List<TradeInOutPrice>();

        // 30分K
        public List<KLine> m_KLdata_30Min = new List<KLine>();

        // 執行檔根目錄
        string mAppPath = "";

#region MACD Indicator

        public iEMA EMA11 = new iEMA(11);
        public iEMA EMA22 = new iEMA(22);

        // Macd 計算器，一開始就要產生
        public iMACD m_iMACD = new iMACD(18, 32, 12);

#endregion

        // 即時的Tick
        TICK m_tick;

        public BEST5 m_best5;

        public TINI _ini_;

        public ServMain parent;

        #region Mode Declerations

        // 是否等待成交回報中
        waitDealMode g_bWaitingDeal = waitDealMode.notWaiting;

        // 當前平倉模式
        cleanupMode g_cleanUpMode = cleanupMode.cleanupMode_KLEnd;

        // 作單模式
        public operationMode g_curMode = operationMode.eAutrMode_DuringMarket;

        // 壓制K線不秀出
        public bool bSupressKline = false;


        // 逆勢單偵測上次啟動時間
        public DateTime g_lastInverseTradeToggleTime = DateTime.Now;

        // 到達攻擊量的K線
        public KLine AttackKL;

        // 移動停損所在K線
        public DateTime lastMovLossTime;

        //進出場次數，用來計算成本
        public int todayTradeTimes = 0;

        //一日進場最多次數
        public const int maxOneDayTradeTimes = 2;

        // 進場當時真正成交的價位
        public int g_LastDealPrice = 0;

        // 今天贏的點數
        public int g_todayWinPoint = 0;
        
        #endregion

        #endregion

        #region Functions
        // 讀取ini設定檔
        private void readIni(string iniPath)
        {
            _ini_ = new TINI(iniPath + "\\config.ini");
            //username = _ini_.getKeyValue("AutoTrading", "Account");
            //password = _ini_.getKeyValue("AutoTrading", "Pass");
            int _mode = int.Parse(_ini_.getKeyValue("AutoTrading", "mode"));
            g_curMode = (_mode == 0) ? operationMode.eAutrMode_FakeTrader : operationMode.eAutrMode_DuringMarket;


        }
        public JNAutoDayTrade_Strategy(String szID , ServMain _p)
        {
            DateTime todate = DateTime.Now;
            parent = _p;
            //上次出手時間
            g_LastTradeTime = new DateTime(todate.Year, todate.Month, todate.Day, 9 , 0, 0);

            mAppPath = szID;

            resetStatus(szID); 
        }
        // 字串比較
        public static bool ContainStr(string source, string toCheck, StringComparison comp = StringComparison.CurrentCultureIgnoreCase)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
        public void readHistoryKLData(string exeRootPth="")
        {
            string ff = mAppPath + @"\HistoryKLine1Min.xml";
            if (exeRootPth.Length > 0)
                ff = exeRootPth;
            using (StreamReader sr = new StreamReader(ff, Encoding.Default, true))
            {
                int Cnt = 0;
                KLine _KL = new KLine(THIRTY_MINUTE, g_CurTime, 0, 0, 0, 0, 0);
                while (sr.Peek() != -1)
                {
                    string _tkstr = sr.ReadLine();
                    if ( _tkstr.Contains(':') && (!_tkstr.Contains('#')) && (!ContainStr(_tkstr , "Tick")) )
                    {
                        string[] klspl1 = _tkstr.Split(':');
                        //:17445.:17445.:17445.:17445.:1 :20141125 21:01
                        int open = int.Parse(klspl1[1].Split('.')[0]);
                        int close = int.Parse(klspl1[2].Split('.')[0]);
                        int highest = int.Parse(klspl1[3].Split('.')[0]);
                        int lowest = int.Parse(klspl1[4].Split('.')[0]);
                        int amount = int.Parse(klspl1[5].Split('.')[0]);

                        int ymd = int.Parse(klspl1[6].Split(' ')[0]);
                        int year = 2015;
                        int month = 1;
                        int day = 1;
                        if (ymd > 20100000)
                        {
                            year = ymd / 10000;
                            month = (ymd - year * 10000) / 100;
                            day = (ymd - year * 10000 - month * 100);
                        }
                        int hour = int.Parse(klspl1[6].Split(' ')[1]);
                        int minute = int.Parse(klspl1[7].Split('=')[0]);

                        g_CurTime = new DateTime(year, month, day, hour, minute, 0);
                        

                        Cnt++;
                        if (Cnt == 1)
                        {
                            _KL = new KLine(THIRTY_MINUTE, g_CurTime, open, highest, lowest, close, amount);
                        }
                        else
                        { 
                            _KL.amount += amount;
                            if (lowest < _KL.lowest)
                                _KL.lowest = lowest;
                            else if (highest > _KL.highest)
                                _KL.highest = highest;

                            if (Cnt == THIRTY_MINUTE/ONE_MINUTE)
                            {
                                _KL.close = close;
                                _KL.datetime = g_CurTime;
                                m_KLdata_30Min.Add(_KL);
                                Cnt = 0;

                                //記錄MACD
                                RecordMACD(_KL);
                               
                            }
                        }
                    }
                    else if (_tkstr.IndexOf("JNM") != -1)
                    {
                        if (_tkstr.IndexOf("PM") != -1)
                            ServMain.OSEJNIPM_ID_CAP = _tkstr;
                        else
                        { 
                            ServMain.OSEJNI_ID_CAP = _tkstr;
                            string strProdYM = ServMain.OSEJNI_ID_CAP.Substring(ServMain.OSEJNI_ID_CAP.IndexOf("JNM") + 3, 4);
                        }
                    }
                }
            }
            historyKLCnt = m_KLdata_30Min.Count;
           
            g_CurTime = DateTime.Now;

            //上午開盤時間，日本時間上午9:00 (台灣時間晚日本1小時)
            g_marketStartTime_AM = new DateTime(g_CurTime.Year, g_CurTime.Month, g_CurTime.Day, 9, 0, 0);

            //上午收盤時間，日本時間下午3:15
            g_marketEndTime_AM = new DateTime(g_CurTime.Year, g_CurTime.Month, g_CurTime.Day, 15, 15, 59);

            //下午開盤時間，日本時間下午4:30
            g_marketStartTime_PM = new DateTime(g_CurTime.Year, g_CurTime.Month, g_CurTime.Day, 16, 29, 59);

            DateTime nxDate = g_CurTime.AddDays(1);

            //下午收盤時間，日本時間上午3:00
            g_marketEndTime_PM = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 3, 0, 0);

            //收盤強制平倉時間，日本時間上午2:50
            g_cleanIntrestTime = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 2, 40, 00);

            if ((ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) || (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
            {
                g_marketStartTime_AM = new DateTime(todate.Year, todate.Month, todate.Day, 9, 0, 0);
                g_marketEndTime_AM = new DateTime(todate.Year, todate.Month, todate.Day, 15, 15, 59);
                g_marketStartTime_PM = new DateTime(todate.Year, todate.Month, todate.Day, 16, 29, 59);
                nxDate = todate.AddDays(1);
                g_marketEndTime_PM = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 3, 0, 0);
                g_cleanIntrestTime = new DateTime(nxDate.Year, nxDate.Month, nxDate.Day, 2, 40, 00);
            }

        }

        // 盤前擷取資訊
        public void resetStatus(string exeRootPth)
        {
            //Force GC
            System.GC.Collect();
            GC.WaitForPendingFinalizers();
            g_todayWinPoint = 0;
            todayTradeTimes = 0;
            g_CurDeposit = 0;
            readHistoryKLData();

            if (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM)
                bDoTrading = true;
            else if (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
            {
                string ff = exeRootPth + @"\AMTicks.xml";
                using (StreamReader sr = new StreamReader(ff, Encoding.Default, true))
                {
                    while (sr.Peek() != -1)
                    {
                        string _tkstr = sr.ReadLine();
                        if (_tkstr.IndexOf("Tick") != -1)
                        {
                            string[] sptk = _tkstr.Split(':');
                            int _t_ = int.Parse(sptk[1].Split('=')[0]);
                            int _price_ = int.Parse(sptk[2].Split(' ')[0]);
                            int _amt_ = int.Parse(sptk[3].Split(' ')[0]);

                            //int hour = _t_ / 10000;
                            //int minute = (_t_ - hour*10000)/100;
                            //int second = (_t_ - hour * 10000 - minute*100 );
                            //g_CurTime = new DateTime(todate.Year, todate.Month, todate.Day, hour, minute, second);
                            //Console.WriteLine(g_CurTime.ToString());


                            TICK tTick = new TICK();
                            tTick.m_nClose = _price_;
                            tTick.m_nQty = _amt_;
                            tTick.m_nTime = _t_;

                            updTime(tTick);
                            collectKLineData(tTick, g_CurTime);
                        }
                    }
                }
                bDoTrading = true;
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

        // 檢查現在時間是否在盤中
        private bool isDuringMarketTime(DateTime _tmToCheck)
        {
            //bool bDuring = (DateTime.Compare(g_CurTime, g_marketStartTime_AM) > 0) && (DateTime.Compare(g_marketEndTime_AM, g_CurTime) > 0);
            return (DateTime.Compare(g_CurTime, g_marketStartTime_AM) > 0) && (DateTime.Compare(g_marketEndTime_PM, g_CurTime) > 0);
        }

     
        // 強制平倉時間
        bool isTimeToCleanInventory1MinK(DateTime _tmToCheck)
        {
            bool bOverTime = (DateTime.Compare(g_CurTime,g_cleanIntrestTime) > 0);

            //如果現在是在盤中，多加一個本機時間判斷
            if((ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM)||(ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
            {
                DateTime nowJpnTime = convertTwTimeToJpn(DateTime.Now);
                bOverTime = bOverTime || (DateTime.Compare(nowJpnTime,g_cleanIntrestTime) > 0);
            }
            return bOverTime;
        }

        public void updTime(TICK tTick)
        {
            int hour = 0;
            int minute = 0;
            int second = 0;
            String cstime = tTick.m_nTime.ToString();
            int strln = cstime.Length;
            if (strln == 1)
            {
                bTimeOver24 = true;
            }
            if (bTimeOver24)
            {
                g_CurTime = g_CurTime.AddSeconds(1);
            }
            else
            {
                if (strln == 5)
                {
                    hour = int.Parse(cstime[0].ToString());
                    minute = int.Parse(cstime.Substring(1, 2));
                    second = int.Parse(cstime.Substring(3, 2));
                }
                else if (strln == 6)
                {
                    hour = int.Parse(cstime.Substring(0, 2));
                    minute = int.Parse(cstime.Substring(2, 2));
                    second = int.Parse(cstime.Substring(4, 2));
                }
            }
            
            //更新當前時間
            g_CurTime = new DateTime(g_CurTime.Year, g_CurTime.Month, g_CurTime.Day, hour, minute, second);
        }

        public DateTime date_RoundDown(DateTime dt, TimeSpan d)
        {
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta);
        }
        #endregion
        #region 當沖
        #region 送出委託
        // 送出委託 nBuySell 0:買進 1:賣出 nTradeType = 0:ROD 1:IOC 2:FOK
        // nDayTrade 0:否 1:是
        private void SendOrder(KLine newestKL, tBuySell nBuySell, bool doStopLoss, KLine prevKL = null, int nTradeType = 4, int nDayTrade = 1) //int nTradeType = 2,int nDayTrade = 0
        {
            todayTradeTimes++;
            if (todayTradeTimes > maxOneDayTradeTimes)
            {
                //Console.WriteLine("一日交易次數不超出" + maxOneDayTradeTimes.ToString() + "次");
                return;
            }
            #region 盤中真單
            if ((ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM) || (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM))
            {
                int nCode = -1;
                int nNewClose = 2;//自動
                string _price;
                string _stoplossPrice;
                string strStockNo = "JNM";
                //if(ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM)
                //    strStockNo = "JNMPM";
                int nSpecialTradeType = 0;//限價
                int iBuySell = nBuySell == tBuySell.Buy ? 0 : 1;
                string strProdYM = "20" + ServMain.OSEJNI_ID_CAP.Substring(ServMain.OSEJNI_ID_CAP.IndexOf("JNM") + 3, 4);
                if (nBuySell == tBuySell.Sell)//要賣，看Bid(買入價)
                {
                    if (doStopLoss)
                    {
                        nNewClose = 0;
                        if (g_CurDeposit > 0) // 加碼倉
                            nQty = (m_best5.m_nBidQty1 >= numContractPerTrade) ? numContractPerTrade : m_best5.m_nBidQty1;
                        else
                            nQty = (m_best5.m_nBidQty1 >= numAddOnPerTime) ? numAddOnPerTime : m_best5.m_nBidQty1;
                    }
                    else  // 平倉，全部平掉
                    {
                        nNewClose = 1;
                        nQty = (m_best5.m_nBidQty1 >= g_CurDeposit) ? g_CurDeposit : m_best5.m_nBidQty1;
                    }
                        
                    _price = (m_best5.m_nBid1 ).ToString();
                    _stoplossPrice = ((newestKL.open + g_MaxStopLoss) ).ToString();


                }
                else //要買，看Ask(買出價)
                {
                    if (doStopLoss)
                    {
                        nNewClose = 0;
                        nQty = (m_best5.m_nAskQty1 >= numContractPerTrade) ? numContractPerTrade : m_best5.m_nAskQty1;
                    }
                    else  // 平倉，全部平掉
                    {
                        nNewClose = 1;
                        nQty = (m_best5.m_nAskQty1 >= g_CurDeposit) ? g_CurDeposit : m_best5.m_nAskQty1;
                    }
                    _price = (m_best5.m_nAsk1 ).ToString();
                    _stoplossPrice = ((newestKL.open - g_MaxStopLoss) ).ToString();
                }

                if (nQty > 0)
                {
                    
                    if (doStopLoss)
                    {
                        if((g_CurDeposit == 0)&&(todayTradeTimes>1)) // 新倉，設置停損點
                            g_LastPrice = 0;//newestKL.open;

                        StringBuilder strMessage = new StringBuilder();
                        strMessage.Length = 1024;
                        Int32 nSiz = 1024;

                        nCode = OrderReply.Functions.SendOverseaFutureOrderAsync(futureAccount, "OSE", strStockNo, strProdYM , iBuySell, nNewClose, nDayTrade, nTradeType, nSpecialTradeType, nQty, _price, "0", "0", "0");
                        if (nCode != 0)
                        {
                            //開啟等待成交回報，帶有停損單的委託一定是建立倉位的委託單
                            g_bWaitingDeal = waitDealMode.waitToBuildInventory;
                            g_lastOrderSentTime = g_CurTime;
                        }
                    }
                    bOrderSent = true;
                }
            }
            #endregion
            #region 盤後模擬單
            //送出模擬單，沒有Best5，直接用買賣盤
            else if (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_Neutural)
            {
                String strRes = "";
                if (doStopLoss) //新倉
                {
                    if ((g_CurDeposit == 0) && (todayTradeTimes > 1)) // 新倉，設置停損點
                        g_LastPrice = 0;//newestKL.open;
                    if (nBuySell == tBuySell.Sell)//要賣，看Bid(買入價)
                    {
                        g_LastDealPrice = m_tick.m_nClose;
                    }
                    else
                    {
                        g_LastDealPrice = m_tick.m_nClose;
                    }

                    string strInventoryTyp = "INV 建倉";
                    if (g_CurDeposit > 0) // 加碼倉
                        strInventoryTyp = "加碼";
                    strRes = strInventoryTyp + "賣出 , ";
                    if (nBuySell == tBuySell.Buy)
                        strRes = strInventoryTyp + "買進 , ";

                    strRes += "時間：" + g_CurTime.ToString() + " 價格：" + g_LastDealPrice.ToString();

                    g_CurDeposit += numContractPerTrade;

                    Console.WriteLine(strRes);
                }
                else //平倉
                {
                    if (nBuySell == tBuySell.Sell)
                    {
                        g_todayWinPoint += (m_tick.m_nClose - g_LastDealPrice) * numContractPerTrade;
                            
                        g_CurDeposit = 0;

                        switch (g_cleanUpMode)
                        {
                            case (cleanupMode.cleanupMode_KLEnd):
                                strRes = "INV 分K結束平倉賣出 時間：" + g_CurTime.ToString() + " 價格：" + m_tick.m_nClose.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                                break;
                            case (cleanupMode.cleanupMode_DayEnd):
                                strRes = "INV 收盤平倉賣出 時間：" + g_CurTime.ToString() + " 價格：" + m_tick.m_nClose.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                                break;
                        }
                        Console.WriteLine(strRes);

                    }
                    else if (nBuySell == tBuySell.Buy)
                    {
                        g_todayWinPoint += (g_LastDealPrice - m_tick.m_nClose) * numContractPerTrade; 
                        g_CurDeposit = 0;

                        switch (g_cleanUpMode)
                        {
                            case (cleanupMode.cleanupMode_KLEnd):
                                strRes = "INV 分K結束平倉買進 時間：" + g_CurTime.ToString() + " 價格：" + m_tick.m_nClose.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                                break;
                            case (cleanupMode.cleanupMode_DayEnd):
                                strRes = "INV 收盤平倉買進 時間：" + g_CurTime.ToString() + " 價格：" + m_tick.m_nClose.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                                break;
                        }
                        Console.WriteLine(strRes);
                    }

                    // 停損
                    if (g_todayWinPoint < -100)
                    {
                        bDoTrading = false;
                        Console.WriteLine("停損，今日停單");
                    }
                }
            }
#endregion

        }

        public void OnReply(DataItem _data)
        {
            String typ = _data.strAgent;

            bool bDeal = (typ == "D");
            bool bCanceled = (typ == "C");



            if (bDeal)
            {
                tBuySell dealBuySell = (_data.strBuySell[0] == 'B') ? tBuySell.Buy : tBuySell.Sell;

                //成交價
                int dealPrice = 0;
                try
                {
                    long _dp = (long.Parse(_data.strPrice));
                    if (_dp < 999999)
                        dealPrice = (int)_dp;
                }
                catch { }

                //成交數量
                int __nqty = 0;
                try { __nqty = int.Parse(_data.strQty); }
                catch { }
                if (__nqty > 0) nQty = __nqty;

                
                // 剛剛送出委託
                if (g_bWaitingDeal != waitDealMode.notWaiting)
                {
                    String _t = _data.strTime;
                    int hour = int.Parse(_t.Substring(0, 2));
                    int minute = int.Parse(_t.Substring(2, 2));
                    int second = int.Parse(_t.Substring(4, 2));

                    DateTime dealtime = new DateTime(todate.Year, todate.Month, todate.Day, hour, minute, second);
                    if (DateTime.Compare(dealtime, g_lastOrderSentTime) >= 0)
                    {
                        // Write Log
                        String strRes = "";
                        if (g_bWaitingDeal == waitDealMode.waitToBuildInventory)
                        {
                            string strInventoryTyp = "建倉";
                            if (g_CurDeposit > 0) // 加碼倉
                                strInventoryTyp = "加碼";
                            strRes = strInventoryTyp + "賣出成交 , ";
                            if (dealBuySell == tBuySell.Buy)
                                strRes = strInventoryTyp + "買進成交 , ";

                            
                            if (g_CurDeposit > 0) // 加碼倉
                                strRes += "時間：" + g_CurTime.ToString() + " 價格：" + dealPrice.ToString();
                            else
                                strRes += "時間：" + g_CurTime.ToString() + " 價格：" + dealPrice.ToString();

                            g_LastDealPrice = dealPrice;
                            g_CurDeposit += nQty;

                            // 建立倉位價格配對表
                            for (int k = 0; k < nQty; k++)
                            {
                                TradeInOutPrice inout = new TradeInOutPrice();
                                inout.In = dealPrice;
                                inout.Out = 0;
                                inOutPriceList.Add(inout);
                            }
                            g_bWaitingDeal = waitDealMode.notWaiting;

                            Console.WriteLine(strRes);
                        }
                        #region 平倉
                        else if (g_bWaitingDeal == waitDealMode.waitToCleanInventory)
                        {
                            g_CurDeposit -= nQty;
                            int _cnt = 0;
                            for (int j = 0; j < inOutPriceList.Count; j++)
                            {
                                if (inOutPriceList[j].Out == 0)
                                {
                                    inOutPriceList[j].Out = dealPrice;
                                    _cnt++;
                                    if (_cnt >= nQty)
                                        break;
                                }
                            }
                            #region 全部平倉完畢
                            // 全部平倉完畢才統計贏的點數
                            if (g_CurDeposit == 0) 
                            {
                                g_bWaitingDeal = waitDealMode.notWaiting;

                                if (dealBuySell == tBuySell.Sell) // 平倉賣出
                                {
                                    //g_todayWinPoint += (dealPrice - g_LastDealPrice) * numContractPerTrade;
                                    //if (g_CurDeposit > numContractPerTrade) // 有加碼倉
                                    //    g_todayWinPoint += (dealPrice - g_LastAddOnDealPrice) * (g_CurDeposit - numContractPerTrade);
                                    for (int j = 0; j < inOutPriceList.Count; j++)
                                    {
                                        if( (inOutPriceList[j].Out != 0) && (inOutPriceList[j].In != 0) )
                                        {
                                            g_todayWinPoint += inOutPriceList[j].Out - inOutPriceList[j].In;
                                        }
                                    }


                                    switch (g_cleanUpMode)
                                    {
                                        case (cleanupMode.cleanupMode_KLEnd):
                                            strRes = "INV 分K結束平倉賣出成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint ).ToString();
                                            break;
                                        case (cleanupMode.cleanupMode_DayEnd):
                                            strRes = "INV 收盤平倉賣出成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint ).ToString();
                                            break;
                                    }

                                }
                                else // 平倉買進
                                {
                                    for (int j = 0; j < inOutPriceList.Count; j++)
                                    {
                                        if ((inOutPriceList[j].Out != 0) && (inOutPriceList[j].In != 0))
                                        {
                                            g_todayWinPoint += inOutPriceList[j].In - inOutPriceList[j].Out;
                                        }
                                    }

                                    switch (g_cleanUpMode)
                                    {
                                        case (cleanupMode.cleanupMode_KLEnd):
                                            strRes = "INV 分K結束平倉買進成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint ).ToString();
                                            break;
                                        case (cleanupMode.cleanupMode_DayEnd):
                                            strRes = "INV 收盤平倉買進成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint ).ToString();
                                            break;
                                    }
                                }

                                // 配對清單清空
                                inOutPriceList.Clear();

                                Console.WriteLine(strRes);

                                // 停損
                                if (g_todayWinPoint < -100)
                                {
                                    bDoTrading = false;
                                    Console.WriteLine("停損，今日停單");
                                }
                            }
                            #endregion
                        }
                        #endregion
                        // 成交成功，代表送出委託結束
                        bOrderSent = false;
                    }
                }
                else // 不是我送出的單，可能遭到強制平倉
                {
                    bool bReluctClean = false;

                    //買進遭到平倉賣出 or 賣出遭到平倉買進 
                    if (
                        ((dealBuySell == tBuySell.Sell) && (g_b1MinTradePositive)) ||
                        ((dealBuySell == tBuySell.Buy) && (!g_b1MinTradePositive))
                      )
                        bReluctClean = true;

                    if (!bReluctClean) return;

                    #region 平倉

                    g_CurDeposit -= nQty;
                    int _cnt = 0;
                    for (int j = 0; j < inOutPriceList.Count; j++)
                    {
                        if (inOutPriceList[j].Out == 0)
                        {
                            inOutPriceList[j].Out = dealPrice;
                            _cnt++;
                            if (_cnt >= nQty)
                                break;
                        }
                    }

                    if (g_CurDeposit == 0)
                    {
                        string strRes = "";
                        g_bWaitingDeal = waitDealMode.notWaiting;

                        String _t = _data.strTime;
                        int hour = int.Parse(_t.Substring(0, 2));
                        int minute = int.Parse(_t.Substring(2, 2));
                        int second = int.Parse(_t.Substring(4, 2));

                        DateTime dealtime = new DateTime(todate.Year, todate.Month, todate.Day, hour, minute, second);

                        if (dealBuySell == tBuySell.Sell) // 平倉賣出
                        {
                            for (int j = 0; j < inOutPriceList.Count; j++)
                            {
                                if ((inOutPriceList[j].Out != 0) && (inOutPriceList[j].In != 0))
                                {
                                    g_todayWinPoint += inOutPriceList[j].Out - inOutPriceList[j].In;
                                }
                            }
                                strRes = "遭到強制平倉賣出成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                        }
                        else // 平倉買進
                        {
                            for (int j = 0; j < inOutPriceList.Count; j++)
                            {
                                if ((inOutPriceList[j].Out != 0) && (inOutPriceList[j].In != 0))
                                {
                                    g_todayWinPoint += inOutPriceList[j].In - inOutPriceList[j].Out;
                                }
                            }
                                strRes = "遭到強制平倉買進成交 時間：" + dealtime.ToString() + " 價格：" + dealPrice.ToString() + " 賺賠：" + (g_todayWinPoint).ToString();
                        }

                        // 配對清單清空
                        inOutPriceList.Clear();

                        Console.WriteLine(strRes);

                        // 停損
                        if (g_todayWinPoint < -100)
                        {
                            bDoTrading = false;
                            Console.WriteLine("停損，今日停單");
                        }
                    }
                    #endregion
                }
            }
            // 被取消單，嘗試繼續送單
            if ((bCanceled) && (g_bWaitingDeal != waitDealMode.notWaiting))
            {
                bOrderSent = false;
            }
        }

        void doBuildInventory(KLine newestKL, bool bAttackKLPositive, KLine prevKL = null, bool bAddOn = false)
        {
            if (bOrderSent) return;
            // 新倉
            if ((g_CurDeposit == 0) && (!bAddOn))
            {
                if (isDuringMarketTime(g_CurTime))
                {
                    if (bAttackKLPositive)
                        SendOrder(newestKL, tBuySell.Buy, true, prevKL);
                    else
                        SendOrder(newestKL, tBuySell.Sell, true, prevKL);

                    g_b1MinTradePositive = bAttackKLPositive;
                    g_LastTradeTime = g_CurTime;
                    lastMovLossTime = newestKL.datetime;
                }
            }
            // 加碼
            else if ((g_CurDeposit > 0) && (g_CurDeposit < g_AvailableDeposit)) //&& bAddOn
            {
                if (isDuringMarketTime(g_CurTime))
                {
                    if (g_b1MinTradePositive)
                        SendOrder(newestKL, tBuySell.Buy, true, prevKL);
                    else
                        SendOrder(newestKL, tBuySell.Sell, true, prevKL);

                    g_LastTradeTime = g_CurTime;
                    lastMovLossTime = newestKL.datetime;
                }
            }
        }

        public void doCleanInventory(KLine newestKL, cleanupMode __mode__ = cleanupMode.cleanupMode_DayEnd)
        {
            if (g_CurDeposit == 0) return;
            if (bOrderSent) return;

            g_cleanUpMode = __mode__;

            if (g_b1MinTradePositive)
                SendOrder(newestKL, tBuySell.Sell, false);
            else
                SendOrder(newestKL, tBuySell.Buy, false);
        }
        #endregion
        #region  出場策略


        // 當沖最後平倉時間
        void dayTradCleanInventory_EndDay(List<KLine> KLineData)
        {
            if (KLineData == null) return;
            if (g_bWaitingDeal != waitDealMode.notWaiting) return;

            // 有倉位，而且到接近收盤時間
            if ((isTimeToCleanInventory1MinK(g_CurTime)) && (g_CurDeposit > 0))
            {
                int cnt = KLineData.Count;
                if (cnt < 1) return;
                KLine newestKL = KLineData[cnt - 1];
                doCleanInventory(newestKL, cleanupMode.cleanupMode_DayEnd);
            }
        }
        

       
        #endregion
        #region 進場策略
        private void DoDayTradeMACD(List<KLine> KLineData)
        {
            if (!bDoTrading) return;
            if (g_bWaitingDeal != waitDealMode.notWaiting) return;
            KLine _KL = KLineData[KLineData.Count - 2];
            double tdiff = (g_CurTime - _KL.datetime).TotalSeconds;
            // 發現已經有新K棒剛剛成形
            if ((tdiff < (THIRTY_MINUTE + 3)) && (tdiff >= THIRTY_MINUTE))
            {
                double _ema11 = EMA11.Value();
                double _ema22 = EMA22.Value();
                double _his = m_iMACD.HistRecords[m_iMACD.HistRecords.Count - 1];
                bool bEmaPositive = _ema11 > _ema22;
                bool bMACDPositive = _his > 0;
                
                if (g_CurDeposit > 0)
                {
                    // 出場，只要macd方向變就出
                    if (g_b1MinTradePositive != bMACDPositive)
                    {
                        doCleanInventory(_KL, cleanupMode.cleanupMode_KLEnd);
                    }
                }
                else
                {
                    // 進場，macd方向跟均線皆同向
                    if (bMACDPositive == bEmaPositive)
                        doBuildInventory(_KL, bMACDPositive);
                }
            }
        }
        #endregion

        #endregion
        #region K棒處理
        public void RecordMACD(KLine _curKL)
        {
            EMA11.ReceiveTick(_curKL.close);
            EMA22.ReceiveTick(_curKL.close);

            double DI = ((double)_curKL.close * 2 + _curKL.highest + _curKL.lowest) / 4.0;
            m_iMACD.ReceiveTick(DI / 100);

            //Console.WriteLine(_curKL.datetime +  " Histogram:" + his.ToString());
        }

        bool insertNewKLine(TICK tTick, DateTime _time, List<KLine> madata, double interval)
        {
            bool bNewKLineInserted = false;

            //最後一根分k的時間轉 time_t
            int cnt = madata.Count;
            if (cnt > historyKLCnt)
            {
                KLine lastK = madata[(cnt - 1)];
                DateTime lastKLineTime = lastK.datetime;

                //現在已超出最後一根k棒的時間
                double timedif = Math.Abs((_time - lastKLineTime).TotalSeconds);

                if (timedif >= interval)
                {
                    DateTime tdate = lastKLineTime.AddSeconds(interval);
                    bNewKLineInserted = true;
                    KLine kl = new KLine(interval, tdate, tTick.m_nClose, tTick.m_nClose,
                        tTick.m_nClose, tTick.m_nClose, tTick.m_nQty);

                    madata.Add(kl);
                }
                else
                {
                    lastK.close = tTick.m_nClose;
                    lastK.amount += tTick.m_nQty;
                    if (tTick.m_nClose < lastK.lowest)
                        lastK.lowest = tTick.m_nClose;
                    else if (tTick.m_nClose > lastK.highest)
                        lastK.highest = tTick.m_nClose;

                    // 上下檔壓力記錄
                    if (tTick.m_nClose == lastK.open)
                        lastK.OpenCloseHitTime++;
                    lastK.tickAmount++;

                }
            }
            else //一根k棒都沒有
            {
                //算出當前tick最靠近的一個區間時間，可以在任意時間開啟監看
                DateTime curbaseTime = date_RoundDown(_time, TimeSpan.FromSeconds(interval));
                KLine kl = new KLine(interval, curbaseTime, tTick.m_nClose, tTick.m_nClose,
                    tTick.m_nClose, tTick.m_nClose, tTick.m_nQty);
                madata.Add(kl);
            }

            return bNewKLineInserted;
        }

        void insertTodayKLine(TICK tTick, KLine _KL)
        {
            if (_KL.open == 0) _KL.open = tTick.m_nClose;
            _KL.close = tTick.m_nClose;
            _KL.amount += tTick.m_nQty;
            if (tTick.m_nClose < _KL.lowest)
                _KL.lowest = tTick.m_nClose;
            else if (tTick.m_nClose > _KL.highest)
                _KL.highest = tTick.m_nClose;
        }


        void collectKLineData(TICK tTick, DateTime curT,bool bTicksGet=true)
        {
            insertTodayKLine(tTick, KLineToday);
            if (insertNewKLine(tTick, curT, m_KLdata_30Min, THIRTY_MINUTE))
            {
                KLine _curK = m_KLdata_30Min[m_KLdata_30Min.Count-2];
                //Console.WriteLine("KL: [op:" + _curK.open.ToString() + "] [cl:" + _curK.close.ToString() + "]");
                RecordMACD(_curK);
                updateGraphData(_curK);
            }

            if (!bTicksGet)
            {
                // K棒結束後2秒內出手(盤中要送單測試暫時關閉)
                //DoDayTradeMACD(m_KLdata_30Min);

                // 不用平倉
                //dayTradCleanInventory_EndDay(m_KLdata_30Min);
            }
            
            //如果等不到回報，表示上次的order沒有成交，應該要繼續嘗試強制平倉
            if(m_KLdata_30Min.Count > 0) 
            {
                if ((ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketPM) || (ServMain.quoteProgramMode == QuoteProgramModeOS.QPM_MarketAM))
                {
                    if (g_bWaitingDeal == waitDealMode.waitToCleanInventory)
                        doCleanInventory(m_KLdata_30Min[m_KLdata_30Min.Count - 1], g_cleanUpMode);
                    else if (g_bWaitingDeal == waitDealMode.waitToBuildInventory)
                        doBuildInventory(AttackKL, g_b1MinTradePositive);   
                }
            }
        }

        void updateGraphData(KLine _curK)
        { 
            //上下Range 45根高低點 + 10%
            int min_rr = 9999999;
            int max_rr = -9999999;
                
            if (m_KLdata_30Min.Count >= parent.m_numBarFocused)
            {
                for (int k = 1; k < (parent.m_numBarFocused+1); k++)
                {
                    KLine curKL = m_KLdata_30Min[m_KLdata_30Min.Count - k];
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
            parent.BeginUpdGraph(_curK, EMA11.Value(), EMA22.Value(),min_rr,max_rr);
        }

        #endregion
        #region ThreadingNotify

        public void Quit()
        {
            ServMain.exit();
        }

        public void OnNotifyServerTime(short sHour, short sMinute, short sSecond, int nTotal)
        {
            g_ServerTime = new DateTime(todate.Year, todate.Month, todate.Day, sHour, sMinute, sSecond);
            double tDiff = Math.Abs((g_ServerTime - g_CurTime).TotalSeconds);
            bool bLessThan1Min = tDiff < ONE_MINUTE;
            //Console.WriteLine("server time diff : " + tDiff.ToString());
        }
        
        public void OnNotifyTicks(TICK tTick)
        {
            m_tick = tTick;
            //先更新tick時間
            updTime(tTick);
            //Console.WriteLine("## Tick ==> " + g_CurTime + " : " + tTick.m_nClose.ToString() + " : " + tTick.m_nQty.ToString());
            if (!(isDuringMarketTime(g_CurTime)))
                return;
            
            collectKLineData(tTick, g_CurTime,false);
            if (parent != null)
                parent.LastCbTime = DateTime.Now;
        }


        public void OnNotifyTicksGet(TICK tTick)
        {
            m_tick = tTick;
            //先更新tick時間
            updTime(tTick);
            if (!(isDuringMarketTime(g_CurTime)))
                return;

            collectKLineData(tTick, g_CurTime);
            if (parent != null)
                parent.LastCbTime = DateTime.Now;
        }


        public void OnNotifyBest5(BEST5 bfi)
        {
            m_best5 = bfi;
        }

        #endregion
        #endregion

    }

}
