@echo off
set username=admin
set password=1

psexec \\192.168.91.128 -u %username% -p %password% -i -d "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\fuzzing_tools\stop_minifuzz.au3"
