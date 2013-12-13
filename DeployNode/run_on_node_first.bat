:: Create the admin without a password, in case the user already exists.
net user /add admin

:: Change the admin account password.
net user admin 1

:: Make the admin an actual administrator.
net localgroup administrators admin /add

:: Disable the firewall.
netsh firewall set opmode disable

:: Disable simple file sharing.
reg add HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa /v forceguest /t reg_dword /d 0 /f

:: Enable automatic logon.
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultUserName /t REG_SZ /d admin /f
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultPassword /t REG_SZ /d 1 /f
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v AutoAdminLogon /t REG_SZ /d 1 /f

:: Restart the computer.
shutdown -r -t 0
