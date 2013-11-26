@echo off
set username=admin
set password=1

psexec \\192.168.91.128 -u %username% -p %password% net share shared="C:\Documents and Settings\admin\Desktop\fuzzing_tools"
net use z: \\192.168.91.128\shared /user:%username% %password%
copy start_minifuzz.au3 Z:\start_minifuzz.au3
copy stop_minifuzz.au3 Z:\stop_minifuzz.au3
psexec \\192.168.91.128 -u %username% -p %password% -i -d "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\Documents and Settings\admin\Desktop\fuzzing_tools\start_minifuzz.au3"
net use z: /delete
psexec \\192.168.91.128 -u %username% -p %password% net share shared /delete
