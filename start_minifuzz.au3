ProcessClose ( "MiniFuzz.exe" )
Run ( "C:\Program Files\Microsoft\MiniFuzz\MiniFuzz.exe", "C:\Program Files\Microsoft\MiniFuzz\" )
WinWaitActive("MiniFuzz")
ControlSetText("MiniFuzz", "", "[NAME:txtExe]", "C:\Program Files\VideoLAN\VLC\vlc.exe")
ControlSetText("MiniFuzz", "", "[CLASS:WindowsForms10.EDIT.app.0.378734a; INSTANCE:3]", "6.5")
ControlClick("MiniFuzz", "", "[NAME:btnFuzzStart]")

While 1
   If WinExists("Error", "") Then
      ControlClick("Error", "", "Yes")
   EndIf
   
   ; Continue fuzzing if large files are found
   If WinExists("[CLASS:#32770; TITLE:Error]") Then
     ControlClick("Error", "", "[CLASS:Button; INSTANCE:1]")
   EndIf
   
   ; Do not remove temporary fuzzed files
   If WinExists("[CLASS:#32770; TITLE:MiniFuzz]") Then
     ControlClick("MiniFuzz", "", "[CLASS:Button; INSTANCE:2]")
   EndIf
   
   ; Exit if MiniFuzz has been closed
   If NOT WinExists("[CLASS:WindowsForms10.Window.8.app.0.378734a; TITLE:MiniFuzz]") Then
     Exit
   EndIf
   
   ; Do not send a crash report
   If WinExists("VLC crash reporting") Then
     WinActivate("VLC crash reporting")
     ControlClick("VLC crash reporting", "", "[CLASS:Button; INSTANCE:2]")
   EndIf

   ; Close Visual C++ Runtime Library error window
   If WinExists("Microsoft Visual C++ Runtime Library") Then
     WinActivate("Microsoft Visual C++ Runtime Library")
     ControlClick("Microsoft Visual C++ Runtime Library", "", "[Text:No]")
   EndIf
Wend
