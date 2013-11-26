@echo off
set username=admin
set password=1
set ip=192.168.91.128

echo copying over triage.py...
psexec \\%ip% -u %username% -p %password% net share shared="C:\fuzzing_tools" >nul 2>&1
net use z: \\%ip%\shared /user:%username% %password% >nul 2>&1
copy triage.py Z:\triage.py >nul 2>&1
net use z: /delete >nul 2>&1
psexec \\%ip% -u %username% -p %password% net share shared /delete >nul 2>&1
echo running triage.py...
psexec \\%ip% -u %username% -p %password% -w C:\fuzzing_tools -i C:\python27\python.exe C:\fuzzing_tools\triage.py "C:\Program Files\VideoLAN\VLC\vlc.exe" C:\minifuzz\crashes >nul 2>&1
