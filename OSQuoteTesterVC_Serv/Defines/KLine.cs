using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuoteTester
{
    public class KLine
    {

        // 間隔，幾分K
        public double interval;

        //時間
        public DateTime datetime;

        //開
        public int open;

        //最高
        public int highest;

        //最低
        public int lowest;

        //收
        public int close;

        //量
        public int amount;


        //打回平盤的次數，代表上下檔壓力
        public int OpenCloseHitTime;


        //總共的Tick
        //tick 數量跟幾個人有關，大量速度盤重點在於有多少人跳進去
        public int tickAmount;


        public KLine(double _interval, DateTime _datetime, int _open, int _highest,
              int _lowest, int _close, int _amount)
        {
            interval = _interval;
            datetime = _datetime;
            open = _open;
            highest = _highest;
            lowest = _lowest;
            close = _close;
            amount = _amount;
        }

        public KLine() 
        {
            interval = 0;
            datetime = DateTime.Now;
            open = 0;
            highest = 0;
            lowest = 0;
            close = 0;
            amount = 0;
        }
    };


    public class TTICK
    {
        public int m_nPtr; // KEY
        public int m_nTime; //時間
        public int m_nBid; // 買價
        public int m_nAsk; // 賣價
        public int m_nClose; //成交價
        public int m_nQty; // 成交量
    }

    public class TickInfo : TTICK
    {
        public DateTime dealTime;
        public TickInfo(int _nBid, int _nAsk, int _nClose, int _nQty , DateTime _dealT)
        { 
            m_nBid = _nBid;
            m_nAsk = _nAsk; 
            m_nClose = _nClose;
            m_nQty = _nQty;
            dealTime = _dealT;
        }
    }
  
    class nlBreakLine : KLine
    {
        public bool bRed; // 新價線紅棒，收高於前3收
        public nlBreakLine (){}
        public nlBreakLine(double _interval, DateTime _datetime, int _open, int _highest,
              int _lowest, int _close, int _amount) : base( _interval,_datetime, _open, _highest,
              _lowest, _close,_amount) 
        {}
    };
}