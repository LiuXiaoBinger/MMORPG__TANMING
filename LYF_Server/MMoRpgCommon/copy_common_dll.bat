@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
rem ============================================================
rem  把共用项目 Debug 目录直接包含的全部 DLL 复制到四服和客户端，包含同目录依赖 DLL
rem  脚本存放位置：LYF_Server\MMoRpgCommon\copy_common_dll.bat
rem  使用下方固定路径，复制时覆盖同名文件，不删除源 DLL
rem ============================================================
set "source=D:\unitypro\LYFMMORGP\LYF_Server\MMoRpgCommon\bin\Debug"
set "serverRoot=D:\unitypro\LYFMMORGP\LYF_Server"
rem 四个服务端输出子目录，均以 serverRoot 指定的服务器根目录为起点
set "targets=CenterServer\bin\Debug MasterServer\bin\Debug GameServer\bin\Debug GateServer\bin\Debug LoginServer\bin\Debug"
rem 客户端共用程序集插件目录，不存在时由脚本创建
set "clientTarget=D:\unitypro\LYFMMORGP\LYF_MMO\Assets\GM_Game\Plugins\Common"

rem 检查源目录是否存在
if not exist "%source%" (
    echo [错误] 源目录不存在: %source%
    echo 请先编译 MMoRpgCommon 项目生成 Debug 输出目录。
    pause
    exit /b 1
)

rem 只统计源目录第一层匹配到的 DLL，数量为零时直接结束
set /a dllCount=0
for %%f in ("%source%\*.dll") do (
    if exist "%%f" set /a dllCount+=1
)
if %dllCount% equ 0 (
    echo [提示] 源目录下没有 .dll 文件: %source%
    pause
    exit /b 0
)

rem 定位本机现有 Visual Studio 构建工具；客户端公共 DLL 必须排除已由 Unity 编译的导航源码。
set "commonBuildTool="
for /f "usebackq delims=" %%m in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "commonBuildTool=%%m"
if not defined commonBuildTool (
    echo [错误] 未找到 Visual Studio MSBuild，未开始分发。
    exit /b 1
)
rem 只发布两个客户端插件位置，服务端仍由下方原有循环复制完整 Debug 程序集。
"%commonBuildTool%" "%~dp0MMoRpgCommon.csproj" /t:BuildAndPublishUnityCommon /p:Configuration=Debug /nologo /v:minimal
if errorlevel 1 exit /b 1

echo [开始] 源目录共 %dllCount% 个 .dll，正在分发复制到 5 个目标目录...
echo.

rem 上方专用目标已成功发布 Common 和 Net 两个客户端公共 DLL。
set /a totalSuccess=2
set /a totalFail=0

rem 逐个服务器复制全部 DLL，每次复制到一个目标算一次成功或失败，不是不同 DLL 的总数
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

rem 客户端其他依赖沿用原复制流程；MMoRpgCommon.dll 已由专用目标发布，不能再用服务端版本覆盖。
echo === LYF_MMO\Assets\GM_Game\Plugins\Common ===
if not exist "%clientTarget%" (
    echo   [信息] 目标目录不存在，正在创建: %clientTarget%
    mkdir "%clientTarget%"
)
for %%f in ("%source%\*.dll") do (
    if exist "%%f" if /i not "%%~nxf"=="MMoRpgCommon.dll" (
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
