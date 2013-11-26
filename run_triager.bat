@echo off
set username=admin
set password=1

psexec \\192.168.91.128 -u %username% -p %password% net share shared="C:\fuzzing_tools"
net use z: \\192.168.91.128\shared /user:%username% %password%
copy triage.py Z:\triage.py
net use z: /delete
psexec \\192.168.91.128 -u %username% -p %password% net share shared /delete
psexec \\192.168.91.128 -u %username% -p %password% -w C:\fuzzing_tools -i -w C:\fuzzing_tools C:\python27\python.exe C:\fuzzing_tools\triage.py "C:\Program Files\VideoLAN\VLC\vlc.exe" C:\minifuzz\crashes