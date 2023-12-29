echo
taskkill /IM "Gorilla Tag.exe"
cd PLUGIN_PATH
rmdir /q MOD_NAME
tar -xf MonkeModList-MOD_NAME.zip
rename "MonkeModList-MOD_NAME" "MOD_NAME"
del MonkeModList-MOD_NAME.zip
pause
