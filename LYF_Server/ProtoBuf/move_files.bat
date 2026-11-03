@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

rem ============================================================
rem  把协议生成输出目录中的匹配项目逐项移动到目标目录，成功后源文件不再保留
rem  移动会覆盖目标同名文件，目标路径由下方 destination 决定
rem ============================================================

rem 当前目标已指向 Network 的 Proto 目录，并非尚未填写的示例路径
set "destination=D:\unitypro\LYFMMORGP\LYF_Server\Network\Proto"
rem ------------------------------------------

set "source=D:\unitypro\LYFMMORGP\LYF_Server\ProtoBuf\out"

rem 检查源目录是否存在
if not exist "%source%" (
    echo [错误] 源目录不存在: %source%
    pause
    exit /b 1
)

rem 仅当目标仍等于代码中的示例占位路径时提示并结束
if "%destination%"=="D:\your\target\path" (
    echo [提示] 请先右键编辑本文件，将 destination 修改为你的实际目标路径。
    pause
    exit /b 0
)

rem 先创建缺失的目标目录，再逐项移动并分别统计成功或失败
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
