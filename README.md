# JNMAutoTrader_Capital
修改自群益api範例的小日經自動交易程式


執行方法: 在x64資料夾內，有au3 script 
需安裝 Autoit 才會自動執行
自動啟動：使用windows工作排程，依序執行

01AllProd_PrvKL.au3 星期一到五上午7點執行，取得今天要交易的月份
					(依據當前月份交易下下個熱門月，避免日本結算日遇假日的問題)
					呼叫 OSQuoteTesterVC.exe，引數1。
					
02TradeAM.au3		星期一到五上午8點前執行，依序呼叫OSQuoteTesterVC_Serv.exe，
					OSQuoteTesterVC.exe，引數3。
					上午盤交易，OSQuoteTesterVC會讀取今日要交易的商品資訊，自動收報價
					透過socket傳給OSQuoteTesterVC_Serv，server收到tick通知，會開始紀錄K
					棒，並啟動分析(這部分可以掛載策略)。
					
					
03AMTick.au3 		星期一到五下午2點30執行，取得該商品上午盤的每筆成交紀錄，引數4。
04TradePM.au3 		星期一到五下午3點30以前執行，同 TradeAM ，只是server不需重開，引數5。

05PMTick.au3 		星期二到六凌晨2點30執行，讀取上午盤&下午盤Tick資料，整併存入JNMHistories目錄，引數6。


OSQuoteTesterVC_Serv 下單，回報，查詢未平倉等功能已經針對群益回覆的格式撰寫完成。
策略位於 JNAutoDayTrade_Strategy.cs 

此套自動交易還在開發中(策略尚未完善)，請輸贏自負，謝謝。