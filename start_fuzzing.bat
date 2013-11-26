@echo off
set username=admin
set password=1
set ip=192.168.91.128

echo copying over start_minifuzz.au3 and stop_minifuzz.au3...
psexec \\%ip% -u %username% -p %password% net share shared="C:\fuzzing_tools" >nul 2>&1
net use z: \\%ip%\shared /user:%username% %password% >nul 2>&1
copy start_minifuzz.au3 Z:\start_minifuzz.au3 >nul 2>&1
copy stop_minifuzz.au3 Z:\stop_minifuzz.au3 >nul 2>&1
net use z: /delete >nul 2>&1
psexec \\%ip% -u %username% -p %password% net share shared /delete >nul 2>&1
echo starting minifuzz...
psexec \\%ip% -u %username% -p %password% -i -d "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\fuzzing_tools\start_minifuzz.au3" >nul 2>&1
