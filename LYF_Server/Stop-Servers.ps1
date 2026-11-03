# 按名称和可执行文件目录查找本项目四服进程，然后强制停止匹配的进程，不执行服务内的正常退出流程。
# 安全边界：只停止 ExecutablePath 位于本脚本所属 LYF_Server 目录内的同名进程，其它项目的同名进程不受影响。

# LYF_Server 根目录（本脚本自身所在目录）
$serverRoot = $PSScriptRoot

# 用于比较的根目录字符串：去掉结尾分隔符并把斜杠统一成反斜杠
$serverRootKey = $serverRoot.TrimEnd('\', '/').Replace('/', '\')

# 待匹配的四个服务可执行文件名
$exeNames = @('CenterServer.exe', 'GameServer.exe', 'GateServer.exe', 'LoginServer.exe')

# 路径筛选后等待停止的 PID 集合，在查找阶段收集，随后统一遍历。
$targetPids = New-Object System.Collections.ArrayList

# 因路径不在本 LYF_Server 目录或读不到路径而跳过的进程数量
$skippedCount = 0

# i 是服务名称列表的下标，依次查询四种服务器进程。
for ($i = 0; $i -lt $exeNames.Length; $i++) {
    # 当前正在处理的可执行文件名
    $exeName = $exeNames[$i]
    # 按进程名查询到的候选进程集合（无匹配时为 $null）
    $procList = Get-CimInstance -ClassName Win32_Process -Filter ("Name = '" + $exeName + "'")
    if ($null -eq $procList) {
        continue
    }
    # 归一化为数组，便于按下标遍历
    $procArray = @($procList)
    # j 是同名候选进程的下标；同名进程可能属于不同项目，需要逐个检查路径。
    for ($j = 0; $j -lt $procArray.Length; $j++) {
        # 单个候选进程对象
        $proc = $procArray[$j]
        # 该进程可执行文件的绝对路径（权限不足时为 $null）
        $exePath = $proc.ExecutablePath
        if ([string]::IsNullOrEmpty($exePath)) {
            $skippedCount = $skippedCount + 1
            Write-Output ('跳过 ' + $exeName + ' PID=' + $proc.ProcessId + '：读不到 ExecutablePath，不做处理')
            continue
        }
        # 可执行文件所在目录（已统一分隔符，用于比较）
        $exeDir = [System.IO.Path]::GetDirectoryName($exePath).TrimEnd('\', '/').Replace('/', '\')
        # 判定是否位于本 LYF_Server 目录或其子目录（忽略大小写，按目录边界）
        $inRoot = $exeDir.Equals($serverRootKey, [System.StringComparison]::OrdinalIgnoreCase) -or $exeDir.StartsWith($serverRootKey + '\', [System.StringComparison]::OrdinalIgnoreCase)
        if ($inRoot) {
            [void]$targetPids.Add([int]$proc.ProcessId)
            Write-Output ('匹配 ' + $exeName + ' PID=' + $proc.ProcessId + ' 路径=' + $exePath)
        }
        else {
            $skippedCount = $skippedCount + 1
            Write-Output ('跳过 ' + $exeName + ' PID=' + $proc.ProcessId + '：不在本 LYF_Server 目录，路径=' + $exePath)
        }
    }
}

if ($targetPids.Count -eq 0) {
    Write-Output ('没有正在运行的本项目四服进程（LYF_Server 目录：' + $serverRoot + '）')
}
else {
    # k 是待停止 PID 列表的下标，只遍历前面路径检查通过的记录。
    for ($k = 0; $k -lt $targetPids.Count; $k++) {
        # 当前要停止的进程 PID
        $pidToStop = [int]$targetPids[$k]
        Stop-Process -Id $pidToStop -Force
        Write-Output ('已停止 PID=' + $pidToStop)
    }
}

# 汇总使用匹配的 PID 数量；跳过计数还包含无法读取路径的进程，不只是不属于本项目的进程。
Write-Output ('处理完成：停止 ' + $targetPids.Count + ' 个，跳过 ' + $skippedCount + ' 个非本项目进程')
