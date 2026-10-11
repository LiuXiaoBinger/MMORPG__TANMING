@echo off
chcp 65001 >nul

rem ============================================
rem  Move Luban output to LYF_Server
rem  output\Code\*.cs (exclude vector*)  ->  ...\LubanManage\Codes
rem  output\Data\*                       ->  ...\LubanManage\Tb
rem  使用 move：移动后 output 目录中的文件会被移除，源目录清空。
rem ============================================

set SRC_CODE=%~dp0output\Code
set SRC_DATA=%~dp0output\Data
set DST_CODE=D:\unitypro\LYFMMORGP\LYF_Server\Network\LubanManage\Codes
set DST_DATA=D:\unitypro\LYFMMORGP\LYF_Server\Network\LubanManage\Tb

if not exist "%DST_CODE%" mkdir "%DST_CODE%"
if not exist "%DST_DATA%" mkdir "%DST_DATA%"



echo ==========================================
echo  Moving Luban output to LYF_Server...
echo ==========================================

echo.
echo [1/2] Move .cs files (exclude vector*) -> Codes
for /f "delims=" %%f in ('dir /b /a-d "%SRC_CODE%\*.cs" ^| findstr /i /v "^vector"') do (
    move /y "%SRC_CODE%\%%f" "%DST_CODE%\" >nul
    echo     moved: %%f
)

echo.
echo [2/2] Move data files -> Tb
for /f "delims=" %%f in ('dir /b /a-d "%SRC_DATA%\*"') do (
    move /y "%SRC_DATA%\%%f" "%DST_DATA%\" >nul
    echo     moved: %%f
)

echo.
echo Done. All files moved.
pause
