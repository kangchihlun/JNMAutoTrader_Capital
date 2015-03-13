///////////////////////////////////////////////////////////
////////          小日經歷史資料回測程序           ////////
///////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml.Serialization;
using _4XLab.NET;
namespace QuoteTester
{
    
    class JNMTradeSim
    {
        #region Parameters

        DateTime g_CurTime;

        //歷史K線數量
        public int historyKLCnt = 0;

        private int ONE_MINUTE = 60;

        private int FIFTEEN_MINUTES = 900;

        private int THIRTY_MINUTES = 1800;

        private int ONE_HOUR = 3600;

        #region 10分K
        // 10分K棒
        private List<KLine> m_KLdata_15Min = new List<KLine>();
        // 10分EMA
        private iEMA m_ema11_15Min = new iEMA(11);
        private iEMA m_ema22_15Min = new iEMA(22);
        // 10分macd
        private iMACD m_macd_15Min = new iMACD(18, 32, 12);
        #endregion 


        #region 30分K
        // 30分K棒
        private List<KLine> m_KLdata_30Min = new List<KLine>();
        // 30分EMA
        private iEMA m_ema11_30Min = new iEMA(11);
        private iEMA m_ema22_30Min = new iEMA(22);
        // 30分macd
        private iMACD m_macd_30Min = new iMACD(18, 32, 12);
        #endregion


        #region 小時K
        // 小時K棒
        private List<KLine> m_KLdata_1Hour = new List<KLine>();
        // 小時EMA
        private iEMA m_ema11_1Hour = new iEMA(11);
        private iEMA m_ema22_1Hour = new iEMA(22);
        // 小時macd
        private iMACD m_macd_1Hour = new iMACD(18, 32, 12);
        #endregion

        #region Trading 相關

        private int g_curDeposite = 0;
        private int g_lastDealPrice = 0;
        private DateTime g_lastDealTime = new DateTime();
        private bool g_bTradePositive = true;

        private int totalTradTimes = 0;
        private int totalWinPnt = 0;

        #endregion

        #endregion

        string m_histPth;
        public JNMTradeSim(string initPath)
        {
            m_histPth = initPath;
            startSim();
        }

         // 字串比較
        public static bool ContainStr(string source, string toCheck, StringComparison comp = StringComparison.CurrentCultureIgnoreCase)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
        public void startSim()
        {
            using (StreamReader sr = new StreamReader(m_histPth, Encoding.Default, true))
            {
                int Cnt_15min = 0;
                int Cnt_30min = 0;
                int Cnt_1Hour = 0;

                KLine _KL_15min = new KLine(FIFTEEN_MINUTES, g_CurTime, 0, 0, 0, 0, 0);
                KLine _KL_30min = new KLine(THIRTY_MINUTES, g_CurTime, 0, 0, 0, 0, 0);
                KLine _KL_1hour = new KLine(ONE_HOUR, g_CurTime, 0, 0, 0, 0, 0);

                while (sr.Peek() != -1)
                {
                    string _tkstr = sr.ReadLine();
                    if ( _tkstr.Contains(':') && (!_tkstr.Contains('#')) && (!ContainStr(_tkstr , "Tick")) )
                    {
                        #region  Parsing Text
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
                        #endregion

                        if (ReceiveData(ref Cnt_15min, ref _KL_15min, ref m_KLdata_15Min,
                                     ref m_ema11_15Min, ref m_ema22_15Min, ref m_macd_15Min,
                                     FIFTEEN_MINUTES, g_CurTime,
                                     open, highest, lowest, close, amount))

                        ReceiveData(ref Cnt_1Hour, ref _KL_1hour, ref m_KLdata_1Hour,
                                     ref m_ema11_1Hour, ref m_ema22_1Hour, ref m_macd_1Hour,
                                     ONE_HOUR, g_CurTime,
                                     open, highest, lowest, close, amount);

                        if( ReceiveData( ref Cnt_30min, ref _KL_30min, ref m_KLdata_30Min,
                                     ref m_ema11_30Min, ref m_ema22_30Min, ref m_macd_30Min,
                                     THIRTY_MINUTES, g_CurTime,
                                     open, highest, lowest, close, amount) )
                        {
                            DoTrendFollowingTrade();
                            doCleanupInventory();                            
                        }
                    }
                }
            }
            Console.WriteLine("============================================================================");
            //Console.WriteLine(" Total Win Point =" + (totalWinPnt).ToString());
            //Console.WriteLine(" money earned =" + ((totalWinPnt / 5) * 140 - (totalTradTimes * 136)).ToString());
        }

        public bool ReceiveData(ref int cnt , ref KLine _KL , ref List<KLine> _lst ,
             ref iEMA _ema11, ref iEMA _ema22, ref iMACD _macd ,
             int period , DateTime _CurTime ,
             int open , int highest , int lowest , int close , int amount)
        {
            cnt++;
            if (cnt == 1)
            {
                _KL = new KLine(FIFTEEN_MINUTES, _CurTime, open, highest, lowest, close, amount);
            }
            else
            {
                _KL.amount += amount;
                if (lowest < _KL.lowest)
                    _KL.lowest = lowest;
                else if (highest > _KL.highest)
                    _KL.highest = highest;

                if (cnt == period / ONE_MINUTE)
                {
                    _KL.close = close;
                    _KL.datetime = g_CurTime;
                    _lst.Add(_KL);
                    cnt = 0;

                    //記錄MACD
                    RecordMACD(_KL, ref _ema11, ref _ema22, ref _macd);

                    //K棒收集完成
                    return true;
                }
            }
            return false;
        }

        public void RecordMACD(KLine _curKL ,ref iEMA ema11 ,ref iEMA ema22 ,ref iMACD macd )
        {
            ema11.ReceiveTick(_curKL.close);
            ema22.ReceiveTick(_curKL.close);

            double DI = ((double)_curKL.close * 2 + _curKL.highest + _curKL.lowest) / 4.0;
            macd.ReceiveTick(DI / 100);
        }

        #region 交易
        public void DoTrendFollowingTrade()
        {
            // 小時線要有方向才做
            if (m_KLdata_1Hour.Count < 22) return;
            if (g_curDeposite > 0) return;

            KLine _curKL30min = m_KLdata_30Min[m_KLdata_30Min.Count - 1];

            // 研判趨勢多空
            bool b30minPositive = m_ema11_30Min.Value() > m_ema22_30Min.Value();
            bool b30minMacdPositive = m_macd_30Min.HistRecords[m_macd_30Min.HistRecords.Count - 1] > 0;
            bool b1hourPositive = m_ema11_1Hour.Value() > m_ema22_1Hour.Value();

            bool bTrendFound = (b30minPositive == b30minMacdPositive) && (b30minMacdPositive == b1hourPositive);

            // 如果30分K棒發生逆趨勢線生長，進場
            bool bDoTrading = false;
            if (bTrendFound)
            {
                double oneThrd =  Math.Abs(m_ema11_30Min.Value() - m_ema22_30Min.Value()) / 3.0;

                if (b30minPositive)
                {
                    if (_curKL30min.close < m_ema11_30Min.Value() - oneThrd)
                        bDoTrading = true;
                }
                else
                {
                    if (_curKL30min.close > m_ema11_30Min.Value() + oneThrd)
                        bDoTrading = true;
                }
            }

            if (bDoTrading)
            {
                g_curDeposite++;
                g_lastDealPrice = _curKL30min.close;
                g_lastDealTime = _curKL30min.datetime;
                g_bTradePositive = b30minPositive;
                totalTradTimes++;

                if (g_bTradePositive)
                    Console.WriteLine("建倉買進 時間：" + _curKL30min.datetime.ToString());
                else
                    Console.WriteLine("建倉賣出 時間：" + _curKL30min.datetime.ToString());
            }
        }

        public void doCleanupInventory()
        {
            if (g_curDeposite == 0) return;
            if (m_KLdata_1Hour.Count < 22) return;

            bool b30minPositive = m_ema11_30Min.Value() > m_ema22_30Min.Value();
            bool b30minMacdPositive = m_macd_30Min.HistRecords[m_macd_30Min.HistRecords.Count - 1] > 0;
            // 任一條件，均線破線 or macd 翻轉
            if ((g_bTradePositive != b30minMacdPositive) || (g_bTradePositive != b30minMacdPositive))
            {
                KLine _curKL30min = m_KLdata_30Min[m_KLdata_30Min.Count - 1];
                g_curDeposite--;
                if (g_bTradePositive)
                {
                    totalWinPnt += _curKL30min.close - g_lastDealPrice - 20;
                    Console.WriteLine("平倉賣出 時間：" + _curKL30min.datetime.ToString() + "賺賠:" + totalWinPnt.ToString());
                }
                else
                {
                    totalWinPnt += g_lastDealPrice - _curKL30min.close - 20;
                    Console.WriteLine("平倉買進 時間：" + _curKL30min.datetime.ToString() + "賺賠:" + totalWinPnt.ToString());
                }
            }
        }
        #endregion
    }
}
