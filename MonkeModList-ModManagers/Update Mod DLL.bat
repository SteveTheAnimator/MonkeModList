echo
taskkill /IM "Gorilla Tag.exe"
cd MOD_PATH
rmdir /s /q MOD_NAME.dll
rename "MonkeModList-MOD_NAME.dll" "MOD_NAME.dll"
taskkill /IM "cmd.exe" /F
pause