@echo off
set username=admin
set password=1
@echo on

psexec \\192.168.91.128 -u %username% -p %password% -i -d "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\Documents and Settings\admin\Desktop\fuzzing_tools\stop_minifuzz.au3"
