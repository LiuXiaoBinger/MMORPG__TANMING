# 用本目录原始 Lua 5.1.4 源码重建 x86/x64 库，中间产物只进入项目 _temp。
param(
    # Visual Studio 的 vcvarsall.bat 所在路径，可在其他开发机显式传入。
    [string]$VcVars = 'C:/Program Files/Microsoft Visual Studio/2022/Professional/VC/Auxiliary/Build/vcvarsall.bat'
)
$ErrorActionPreference = 'Stop'
# 从正式第三方目录定位仓库，不依赖执行命令时的当前位置。
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
# 每种平台的中间对象/日志独立保存，禁止输出到客户端或服务端根目录。
$work = Join-Path $repository '_temp/moshenta-lua51-build'
# 原 Makefile CORE_O 与 LIB_O 的29个模块，排除命令行解释器和luac。
$modules = 'lapi lcode ldebug ldo ldump lfunc lgc llex lmem lobject lopcodes lparser lstate lstring ltable ltm lundump lvm lzio lauxlib lbaselib ldblib liolib lmathlib loslib ltablib lstrlib loadlib linit'.Split(' ')
# AnyCPU在进程实际启动后选择相应原生库，不改变GameServer平台设置。
$architectures = @('x86', 'x64')
for ($platformIndex = 0; $platformIndex -lt $architectures.Count; $platformIndex++) {
    # 当前原生ABI及独立对象目录。
    $architecture = $architectures[$platformIndex]
    $directory = Join-Path $work $architecture
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    # 响应文件的/link选项必须和后续链接参数位于同一行。
    $arguments = @('/nologo', '/LD', '/O2', '/MD', '/DLUA_BUILD_AS_DLL', '/D_CRT_SECURE_NO_WARNINGS')
    $arguments += '/I"' + (Join-Path $PSScriptRoot 'src') + '"'
    for ($moduleIndex = 0; $moduleIndex -lt $modules.Count; $moduleIndex++) {
        $arguments += '"' + (Join-Path $PSScriptRoot ('src/' + $modules[$moduleIndex] + '.c')) + '"'
    }
    $arguments += @('/link', '/OUT:"lua51.dll"', '/IMPLIB:"lua51.lib"')
    $response = Join-Path $directory 'lua51.rsp'
    [IO.File]::WriteAllText($response, ($arguments -join ' '), [Text.Encoding]::ASCII)
    # vcvars只作用于该构建子进程，不改变用户环境。
    $batch = Join-Path $directory 'build.bat'
    $body = "@echo off`r`ncall `"$VcVars`" $architecture`r`nif errorlevel 1 exit /b %errorlevel%`r`ncl @`"$response`"`r`nexit /b %errorlevel%`r`n"
    [IO.File]::WriteAllText($batch, $body, [Text.Encoding]::ASCII)
    Push-Location $directory
    try {
        & cmd.exe /d /c $batch *> (Join-Path $directory 'build.log')
        if ($LASTEXITCODE -ne 0) { throw "Lua $architecture 编译失败，见 $directory/build.log" }
    }
    finally { Pop-Location }
    # 仅最终运行库进入正式第三方部署目录；源码、许可和脚本可共同复建。
    $destination = Join-Path $PSScriptRoot $architecture
    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Copy-Item -LiteralPath (Join-Path $directory 'lua51.dll') -Destination (Join-Path $destination 'lua51.dll') -Force
    Write-Output "Lua 5.1.4 $architecture 编译成功"
}
