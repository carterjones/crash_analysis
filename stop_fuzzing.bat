@echo off
set username=admin
set password=1
set ip=192.168.91.128

psexec \\%ip% -u %username% -p %password% -i "C:\Program Files\AutoIt3\AutoIt3.exe" "C:\fuzzing_tools\stop_minifuzz.au3"
