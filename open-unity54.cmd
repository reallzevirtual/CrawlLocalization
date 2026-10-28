@echo off
setlocal
set "OPENSSL_ia32cap=0:0"
start "" "C:\Program Files\Unity\Hub\Editor\5.4.2f2\Editor\Unity.exe" -projectPath "%~dp0FontBundleProject"
endlocal
