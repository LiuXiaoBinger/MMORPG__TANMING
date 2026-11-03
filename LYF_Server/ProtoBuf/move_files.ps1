# ============================================================
#  将协议生成输出目录第一层文件移动到目标目录；成功移动后源文件不再保留。
#  当前目标仍是占位路径，运行时会提示结束；实际移动使用 Force，可能覆盖目标同名文件。
# ============================================================

# 目标目录的配置位置；必须先改成实际目标才会进入移动流程。
$destination = "D:\your\target\path"
# 协议生成器的输出目录，下面只枚举这一层文件。

$source = "D:\unitypro\LYFMMORGP\LYF_Server\ProtoBuf\out"

# 检查源目录是否存在
if (-not (Test-Path -Path $source)) {
    Write-Host "[错误] 源目录不存在: $source" -ForegroundColor Red
    exit 1
}

# 检查目标路径是否已填写（仍是默认值则提示）
if ($destination -eq "D:\your\target\path") {
    Write-Host "[提示] 请先打开本脚本，将 `$destination 修改为你的实际目标路径。" -ForegroundColor Yellow
    exit 0
}

# 目标目录不存在则自动创建
if (-not (Test-Path -Path $destination)) {
    Write-Host "[信息] 目标目录不存在，正在创建: $destination" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
}

# 枚举源目录第一层默认可见的文件；不进入子目录，也未启用隐藏项目枚举。
$files = Get-ChildItem -Path $source -File

if ($files.Count -eq 0) {
    Write-Host "[提示] 源目录下没有文件可转移。" -ForegroundColor Yellow
    exit 0
}

Write-Host "[开始] 共找到 $($files.Count) 个文件，正在转移到: $destination" -ForegroundColor Green

# 已成功移动的文件数；只有 Move-Item 未抛异常才增加。
$successCount = 0
# 移动失败的文件数；单个文件失败后继续处理下一个。
$failCount = 0

# file 是当前枚举到的文件信息，保持原文件名构造目标完整路径。
foreach ($file in $files) {
    # 当前文件的目标路径，源文件和目标文件使用同一个文件名。
    $destPath = Join-Path -Path $destination -ChildPath $file.Name
    try {
        Move-Item -Path $file.FullName -Destination $destPath -Force -ErrorAction Stop
        Write-Host "  [OK] $($file.Name)" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "  [失败] $($file.Name) - $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "[完成] 成功转移 $successCount 个，失败 $failCount 个。" -ForegroundColor Cyan
