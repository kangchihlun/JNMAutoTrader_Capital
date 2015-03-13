Func _WinWait ($_Title,$_Text = "")
    If $_Title = "" Then
        Return 0
    Else
        Do
        Until WinExists ($_Title,$_Text)
		Return 1
    EndIf
EndFunc
Func InitQuoteTesterVC()
	Run(@ScriptDir&"\\OSQuoteTesterVC_Serv.exe 3")
	Local $res= _WinWait("OSQuote_Server","")
	if $res == 1 then
		Sleep(10000)
		ControlClick("OSQuote_Server", "Initialize" ,"[NAME:btnInitialize]") 
		Sleep(10000)
		
		Run(@ScriptDir&"\\OSQuoteTesterVC.exe 3")
		Local $res2= _WinWait("OSQuote","")
		if $res == 1 then
			Sleep(10000)
			ControlClick("OSQuote", "Initialize" ,"[NAME:btnInitialize]") 
			Sleep(10000)
			ControlClick("OSQuote", "Go" ,"[NAME:btnGo]")
		endif
	endif
EndFunc


InitQuoteTesterVC()


