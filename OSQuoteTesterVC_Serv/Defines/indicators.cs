//
// 4X Lab.NET ?Copyright 2005-2006 ASCSE LLC
// http://www.4xlab.net
// email: 4xlab@4xlab.net
//

using System;
using System.Collections.Generic;
namespace _4XLab.NET
{
	public class iEMA
	{
		private int tickcount;
		private int periods;
		private double dampen;
		private double emav;

        // 線資料
        public List<double> records;

		public iEMA(int pPeriods)
		{
			periods = pPeriods;
			dampen  = 2/((double)1.0+periods);
            records = new List<double>();
		}

		public void ReceiveTick(double Val)
		{
			if (tickcount < periods)
				emav += Val;
			if (tickcount ==periods)
				emav /= periods;
			if (tickcount > periods)
				emav = (dampen*(Val-emav))+emav;

			if (tickcount <= (periods+1) )
			{			
				// avoid overflow by stopping use of tickcount
				// when indicator is fully primed
				tickcount++;
			}

            // 接收同時寫入紀錄
            records.Add(Value());
		}

        public void Reset()
        {
            dampen = 2 / ((double)1.0 + periods);
            records.Clear();
        }

		public double Value()
		{
			double v;

			if (isPrimed())
				v = emav;
			else
				v = 0;

			return v;
		}

		public bool isPrimed()
		{	
			bool v = false;
			if (tickcount > periods)
			{
				v = true;
			}
			return v;
		}
	}

	public class iMACD
	{
		int pSlowEMA, pFastEMA, pSignalEMA;
		iEMA slowEMA, fastEMA, signalEMA;

        // 目前只提供柱資料紀錄
        public List<double> HistRecords;

		// restriction: pPFastEMA < pPSlowEMA
		public iMACD(int pPFastEMA, int pPSlowEMA, int pPSignalEMA)
		{
			pFastEMA = pPFastEMA;
			pSlowEMA = pPSlowEMA;
			pSignalEMA = pPSignalEMA;

			slowEMA = new iEMA(pSlowEMA);
			fastEMA = new iEMA(pFastEMA);
			signalEMA = new iEMA(pSignalEMA);

            HistRecords = new List<double>();
		}

		public void ReceiveTick(double Val)
		{
			slowEMA.ReceiveTick(Val);
			fastEMA.ReceiveTick(Val);

			if (slowEMA.isPrimed() && fastEMA.isPrimed())
			{
				signalEMA.ReceiveTick(fastEMA.Value()-slowEMA.Value());
			}


            #region 接收同時寫入紀錄
            double hist = 0;
            if (signalEMA.isPrimed())
            {
                double MACD = fastEMA.Value() - slowEMA.Value();
                double signal = signalEMA.Value();
                hist = MACD - signal;
            }
            HistRecords.Add(hist);
            #endregion
        }

		public void Value(out double MACD, out double signal, out double hist)
		{
			if (signalEMA.isPrimed())
			{
				MACD = fastEMA.Value() - slowEMA.Value();
				signal = signalEMA.Value();
				hist = MACD - signal;
			}
			else
			{
				MACD = 0;
				signal = 0;
				hist = 0;
			}
		}

		public bool isPrimed()
		{	
			bool v = false;
			if (signalEMA.isPrimed())
			{
				v = true;
			}
			return v;
		}

        public void Reset()
        { 
            slowEMA.Reset();
            fastEMA.Reset();
            signalEMA.Reset();
            HistRecords.Clear();
        }
	}

}

