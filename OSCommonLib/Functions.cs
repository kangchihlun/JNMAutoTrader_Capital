using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace OSCommonLib
{
    //小日經做單模式，依照啟動時間排序
    //以下描述為台灣時間
    public enum QuoteProgramModeOS
    {
        QPM_Neutural = 0,     // 0.一般雙擊開啟的模式(default)  
        QPM_AllProduct,       // 1.取得當前商品列表(需每日更新，藉此反推出凱基這邊的代碼)
        QPM_MarketKLGetter,   // 2.每日上午7點50取得昨日下午盤K棒資訊
        QPM_MarketAM,         // 3.小日經上午盤中作單模式
        QPM_AMMarketTickGet,  // 4.小日經上午盤後Tick資訊擷取
        QPM_MarketPM,         // 5.小日經下午盤作單模式
        QPM_AfterMarket,      // 6.小日經下午盤後Tick資訊擷取，寫入每日歷史紀錄
    }
    public struct KLineStr
    {
        public string _str;
        public DateTime date;
    }
    // Define Call Back Function
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnConnect(int nKind, int nErrorCode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnOverseaProducts([MarshalAs(UnmanagedType.BStr)]string strProducts);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnGetStockIdx(short nCode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnNotifyTicks(short sStockidx, int nPtr);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnNotifyServerTime(short sHour, short sMinute, short sSecond);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnNotifyKLineData([MarshalAs(UnmanagedType.BStr)]string strStockNo, [MarshalAs(UnmanagedType.BStr)]string strKData);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void FOnNotifyTicksGet( short sStockidx,int nPtr, int nTime, int nClose, int nQty);

    public struct FOREIGN
    {
        public short m_sStockidx;				// 系統自行定義的股票代碼
        public short m_sDecimal;				// 報價小數位數
        public int m_nDenominator;			// 分母

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
        public string m_cMarketNo;              // 市場代號0x05海外商品

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string m_caExchangeNo;          // 交易所代號

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string m_caExchangeName;         // 交易所名稱

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string m_caStockNo;              // 股票代號

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string m_caStockName;                 // 股票名稱

        public int m_nOpen;					// 開盤價
        public int m_nHigh;					// 最高價
        public int m_nLow;					// 最低價
        public int m_nClose;					// 成交價
        public int m_dSettlePrice;           //結算價
        public int m_nTickQty;				    // 單量
        public int m_nRef;					// 昨收、參考價

        public int m_nBid;					// 買價
        public int m_nBc;					    // 買量
        public int m_nAsk;					// 賣價
        public int m_nAc;					    // 賣量

        public int m_nTQty;					    // 總量
    }

    [Serializable]
    public struct TICK
    {
        public int m_nPtr;			//Index
        public int m_nTime;        // 時間
        public int m_nClose;		//成交價
        public int m_nQty;			//成交量
    }

    [Serializable]
    public struct BEST5
    {
        public int m_nBid1;
        public int m_nBidQty1;
        public int m_nBid2;
        public int m_nBidQty2;
        public int m_nBid3;
        public int m_nBidQty3;
        public int m_nBid4;
        public int m_nBidQty4;
        public int m_nBid5;
        public int m_nBidQty5;
        public int m_nAsk1;
        public int m_nAskQty1;
        public int m_nAsk2;
        public int m_nAskQty2;
        public int m_nAsk3;
        public int m_nAskQty3;
        public int m_nAsk4;
        public int m_nAskQty4;
        public int m_nAsk5;
        public int m_nAskQty5;
    }

    public enum ApiMessage 
    {
        SK_SUCCESS                                      = 0,
        SK_FAIL                                         = -1,
        SK_ERROR_SERVER_NOT_CONNECTED					= -3,
        SK_ERROR_INITIALIZE_FAIL						= -4,
        SK_ERROR_ACCOUNT_NOT_EXIST						= 1,
        SK_ERROR_ACCOUNT_MARKET_NOT_MATCH				= 2,
        SK_ERROR_PERIOD_OUT_OF_RANGE					= 3,
        SK_ERROR_FLAG_OUT_OF_RANGE						= 4,
        SK_ERROR_BUYSELL_OUT_OF_RANGE					= 5,
        SK_ERROR_ORDER_SERVER_INVALID					= 6,
        SK_ERROR_PERMISSION_DENIED				        = 7,
        SK_KLINE_DATA_TYPE_NOT_FOUND			        = 8,
        SK_ERROR_PERMISSION_TIMEOUT			            = 9,

        SK_SUBJECT_CONNECTION_CONNECTED			        = 100,
        SK_SUBJECT_CONNECTION_DISCONNECT		        = 101,
        SK_SUBJECT_QUOTE_PAGE_EXCEED			        = 200,
        SK_SUBJECT_QUOTE_PAGE_INCORRECT			        = 201,
        SK_SUBJECT_TICK_PAGE_EXCEED				        = 210,
        SK_SUBJECT_TICK_PAGE_INCORRECT			        = 211,
        SK_SUBJECT_TICK_STOCK_NOT_FOUND			        = 212,
        SK_SUBJECT_BEST5_DATA_NOT_FOUND		        	= 213,
        SK_SUBJECT_QUOTEREQUEST_NOT_FOUND		    	= 214,
        SK_SUBJECT_SERVER_TIME_NOT_FOUND		    	= 215,

        SK_WARNING_LOGIN_ALREADY						= 2003
    }

    public class Functions
    { 
        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_Initialize", CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_Initialize(string pcUserName, string pcPassword);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_EnterMonitor", CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_EnterMonitor(short nConnectType);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_LeaveMonitor", CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_LeaveMonitor();

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_RequestStocks", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_RequestStocks(out int psPageNo, string pStockNos);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_RequestTicks", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_RequestTicks(out int psPageNo, string pStockNos);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_GetStockByIndex", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_GetStockByIndex(short sIndex, out FOREIGN TForeign);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_GetTick", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKQuoteLib_GetTick(short sIndex, int nPtr, out TICK TTick);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_GetBest5", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_GetBest5(short sIndex, out BEST5 TBest5);

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_RequestOverseaProducts", CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_RequestOverseaProducts();

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_RequestServerTime", CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_RequestServerTime();

        [DllImport("SKOSQuoteLib.dll", EntryPoint = "SKOSQuoteLib_RequestKLine", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern int SKOSQuoteLib_RequestKLine(string pStockNos, short KLineType);




        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachConnectCallBack(FOnConnect Connect);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachQuoteCallBack(FOnGetStockIdx StockIdx);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachTicksCallBack(FOnNotifyTicks StockIdx);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachBest5CallBack(FOnGetStockIdx StockIdx);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachOverseaProductsCallBack(FOnOverseaProducts OverseaProducts);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachServerTimeCallBack(FOnNotifyServerTime ServerTime);
        
        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachKLineDataCallBack(FOnNotifyKLineData KLineData);

        [DllImport("SKOSQuoteLib.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SKOSQuoteLib_AttachHistoryTicksGetCallBack(FOnNotifyTicksGet TicksGet);

        
    }
}
