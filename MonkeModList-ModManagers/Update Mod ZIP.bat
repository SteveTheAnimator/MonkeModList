echo
taskkill /IM "Gorilla Tag.exe"
cd PLUGIN_PATH
rmdir /s /q MOD_NAME
tar -xf MOD_NAME.zip
del MOD_NAME.zip
pause
