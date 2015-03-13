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
   Run(@ScriptDir&"\\OSQuoteTesterVC.exe 1")
   Local $res= _WinWait("OSQuote","")
   if $res == 1 then
   Sleep(10000)
	ControlClick("OSQuote", "Initialize" ,"[NAME:btnInitialize]")
	Sleep(10000)
	ControlClick("OSQuote", "Go" ,"[NAME:btnGo]")
	endif
EndFunc


InitQuoteTesterVC()


