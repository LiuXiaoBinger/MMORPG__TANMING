# 为开发开服生成可复用的运行目录。ServerRoot 为服务端工程根目录，Configuration 为构建配置，
# Name 为单个服务工程名，BuildDir 为该服务刚刚编译完成的输出目录。
function Get-RealmRuntimeProgram([string]$ServerRoot, [string]$Configuration, [string]$Name, [string]$BuildDir) {
    # 按相对文件名与内容标识整套程序，配置表或依赖单独变化也会启用新版本。
    $files = @(Get-ChildItem -LiteralPath $BuildDir -File -Recurse -Force | Sort-Object FullName)
    # 依次追加每个相对路径和 SHA256，用于比较两次构建的所有运行输入。
    $manifest = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $files.Count; $i++) {
        # i 是构建文件下标，路径只保留服务目录内的部分，供同版进程复用。
        $relative = $files[$i].FullName.Substring($BuildDir.Length + 1)
        [void]$manifest.AppendLine($relative + '|' + (Get-FileHash -LiteralPath $files[$i].FullName -Algorithm SHA256).Hash)
    }
    # 将清单摘要用作目录名，各服务只在实际内容变化时生成一份新副本。
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        # 内容摘要不带分隔符，供启动与关服识别同一版本目录。
        $digest = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($manifest.ToString()))).Replace('-', '')
    } finally { $sha.Dispose() }
    # 运行副本与编译缓存分开，运行中或关闭后仍被 Windows 占用的二进制都无需覆盖。
    $serviceDir = Join-Path (Split-Path -Parent $ServerRoot) "_temp/realm-runtime/$Configuration/$Name"
    # 版本目录可供同一构建配置下多个区共用，程序名仍与工程一致。
    $versionDir = Join-Path $serviceDir $digest
    $exe = Join-Path $versionDir "$Name.exe"
    # 部分 Windows 进程会在文件刚复制好时占用它。只发布一个完成标记，不重命名含 DLL 的目录。
    $ready = Join-Path $versionDir '.ready'
    if (![IO.File]::Exists($ready)) {
        # 同机同时开区时，同一个内容版本只由一个启动器写入。
        $writer = [Threading.Mutex]::new($false, "Local\LYF-Realm-$Name-$digest")
        try {
            try { [void]$writer.WaitOne() }
            catch [Threading.AbandonedMutexException] { } # 上次开服中止，本进程已取得写入权。
            if (![IO.File]::Exists($ready)) {
                [void][IO.Directory]::CreateDirectory($versionDir)
                # 每项只复制到当前工程版本；失败后下次仍可补全同一份程序。
                $entries = @(Get-ChildItem -LiteralPath $BuildDir -Force)
                for ($i = 0; $i -lt $entries.Count; $i++) {
                    Copy-Item -LiteralPath $entries[$i].FullName -Destination $versionDir -Recurse -Force -ErrorAction Stop
                }
                [void](Get-Item -LiteralPath $exe -ErrorAction Stop)
                [IO.File]::WriteAllText($ready, '')
            }
        } finally { $writer.ReleaseMutex(); $writer.Dispose() }
    }
    if ($Name -eq 'MasterServer') {
        # 公共频道缓存和好友语音按原构建配置持久保存，独立程序目录继续使用既有的文件。
        $audioFolders = @('ChatAudioCache', 'SocialChatAudio')
        for ($i = 0; $i -lt $audioFolders.Count; $i++) {
            # 原服务器按构建配置保存的实际录音目录。
            $audioDir = Join-Path $ServerRoot "MasterServer/bin/$Configuration/$($audioFolders[$i])"
            [void][IO.Directory]::CreateDirectory($audioDir)
            # 每份程序完成发布后关联原语音库，已有版本复用同一个目录连接。
            $linkPath = Join-Path $versionDir $audioFolders[$i]
            if (!(Test-Path -LiteralPath $linkPath -PathType Container)) {
                [void](New-Item -ItemType Junction -Path $linkPath -Target $audioDir -Force -ErrorAction Stop)
            }
        }
    }
    return $exe
}

# 把本项目开发运行副本映射回同一服务的部署路径，供现有关服入口按配置和服务名筛选。
# ServerRoot 为本项目服务端目录，ExePath 为 Windows 返回的进程完整路径。
function Get-RealmDeploymentPath([string]$ServerRoot, [string]$ExePath) {
    # 严格限定项目、构建配置、工程名、内容摘要和同名程序，其他位置按原路径处理。
    $runtimeRoot = Join-Path (Split-Path -Parent $ServerRoot) '_temp\realm-runtime'
    $pattern = '^' + [regex]::Escape($runtimeRoot) + '\\(Debug|Release)\\([^\\]+)\\[0-9A-F]{64}\\\2\.exe$'
    if ($ExePath -match $pattern) { return [IO.Path]::GetFullPath((Join-Path $ServerRoot "$($Matches[2])/bin/$($Matches[1])/$($Matches[2]).exe")) }
    return $ExePath
}
