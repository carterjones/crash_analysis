@echo off
set username=admin
set password=1
set ip=192.168.91.128

psexec \\%ip% -u %username% -p %password% net share shared="C:\fuzzing_tools"
net use z: \\%ip%\shared /user:%username% %password%
copy start_minifuzz.au3 Z:\start_minifuzz.au3
copy stop_minifuzz.au3 Z:\stop_minifuzz.au3
net use z: /delete
psexec \\%ip% -u %username% -p %password% net share shared /delete
psexec \\%ip% -u %username% -p %password% -i -d "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\fuzzing_tools\start_minifuzz.au3"
