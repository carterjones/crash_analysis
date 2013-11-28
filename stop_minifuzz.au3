ProcessClose ( "MiniFuzz.exe" )
DirRemove ( "C:\minifuzz\temp" , 1 )
While ProcessExists ( "vlc.exe" )
   ProcessClose ( "vlc.exe" )
WEnd
