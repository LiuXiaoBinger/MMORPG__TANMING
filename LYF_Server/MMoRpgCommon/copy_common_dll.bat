@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
rem ============================================================
rem  将 MMoRpgCommon\bin\Debug 下的 .dll 分发复制到各 Server 及客户端
rem  脚本存放位置：LYF_Server\MMoRpgCommon\copy_common_dll.bat
rem  双击即可运行
rem ============================================================
set "source=D:\unitypro\LYFMMORGP\LYF_Server\MMoRpgCommon\bin\Debug"
set "serverRoot=D:\unitypro\LYFMMORGP\LYF_Server"
rem 服务端目标列表（相对 serverRoot）
set "targets=CenterServer\bin\Debug GameServer\bin\Debug GateServer\bin\Debug LoginServer\bin\Debug"
rem Unity客户端插件目录
set "clientTarget=D:\unitypro\LYFMMORGP\LYF_MMO\Assets\GM_Game\Plugins\Common"

rem 检查源目录是否存在
if not exist "%source%" (
    echo [错误] 源目录不存在: %source%
    echo 请先编译 MMoRpgCommon 项目生成 Debug 输出目录。
    pause
    exit /b 1
)

rem 统计dll数量
set /a dllCount=0
for %%f in ("%source%\*.dll") do (
    if exist "%%f" set /a dllCount+=1
)
if %dllCount% equ 0 (
    echo [提示] 源目录下没有 .dll 文件: %source%
    pause
    exit /b 0
)

echo [开始] 源目录共 %dllCount% 个 .dll，正在分发复制到 5 个目标目录...
echo.

set /a totalSuccess=0
set /a totalFail=0

rem ---------- 复制到4个Server服务端 ----------
for %%t in (%targets%) do (
    set "dest=%serverRoot%\%%t"
    echo === %%t ===
    if not exist "!dest!" (
        echo   [信息] 目标目录不存在，正在创建: !dest!
        mkdir "!dest!"
    )
    for %%f in ("%source%\*.dll") do (
        if exist "%%f" (
            copy /y "%%f" "!dest!\" >nul 2>&1
            if !errorlevel! equ 0 (
                echo   [OK] %%~nxf
                set /a totalSuccess+=1
            ) else (
                echo   [失败] %%~nxf
                set /a totalFail+=1
            )
        )
    )
    echo.
)

rem ---------- 复制到客户端 Plugins\Common ----------
echo === LYF_MMO\Assets\GM_Game\Plugins\Common ===
if not exist "%clientTarget%" (
    echo   [信息] 目标目录不存在，正在创建: %clientTarget%
    mkdir "%clientTarget%"
)
for %%f in ("%source%\*.dll") do (
    if exist "%%f" (
        copy /y "%%f" "%clientTarget%\" >nul 2>&1
        if !errorlevel! equ 0 (
            echo   [OK] %%~nxf
            set /a totalSuccess+=1
        ) else (
            echo   [失败] %%~nxf
            set /a totalFail+=1
        )
    )
)

echo.
echo [完成] 共复制到 5 个目标目录，成功 !totalSuccess! 个文件，失败 !totalFail! 个。
pause
