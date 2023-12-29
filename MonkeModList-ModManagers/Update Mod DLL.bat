echo
taskkill /IM "Gorilla Tag.exe"
cd PLUGIN_PATH
del /q MOD_NAME.dll
rename "MonkeModList-MOD_NAME.dll" "MOD_NAME.dll"
pause
