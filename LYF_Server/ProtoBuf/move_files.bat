@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

rem ============================================================
rem  ProtoBuf out 文件夹文件转移脚本
rem  用法：修改下面的 destination 为你的目标路径，然后双击运行
rem ============================================================

rem ---------- 请在这里填写目标路径 ----------
set "destination=D:\unitypro\LYFMMORGP\LYF_Server\Network\Proto"
rem ------------------------------------------

set "source=D:\unitypro\LYFMMORGP\LYF_Server\ProtoBuf\out"

rem 检查源目录是否存在
if not exist "%source%" (
    echo [错误] 源目录不存在: %source%
    pause
    exit /b 1
)

rem 检查是否已填写目标路径
if "%destination%"=="D:\your\target\path" (
    echo [提示] 请先右键编辑本文件，将 destination 修改为你的实际目标路径。
    pause
    exit /b 0
)

rem 目标目录不存在则自动创建
if not exist "%destination%" (
    echo [信息] 目标目录不存在，正在创建: %destination%
    mkdir "%destination%"
)

echo [开始] 正在转移文件到: %destination%
echo.

set /a successCount=0
set /a failCount=0

for %%f in ("%source%\*.*") do (
    if exist "%%f" (
        move /y "%%f" "%destination%\" >nul 2>&1
        if !errorlevel! equ 0 (
            echo   [OK] %%~nxf
            set /a successCount+=1
        ) else (
            echo   [失败] %%~nxf
            set /a failCount+=1
        )
    )
)

echo.
echo [完成] 成功转移 !successCount! 个，失败 !failCount! 个。
pause
