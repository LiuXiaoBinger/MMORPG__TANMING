# ============================================================
#  ProtoBuf out 文件夹文件转移脚本
#  用法：修改下面的 $destination 为你的目标路径，然后运行本脚本
# ============================================================

# ---------- 请在这里填写目标路径 ----------
$destination = "D:\your\target\path"
# ------------------------------------------

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

# 获取源目录下的所有文件（不包含子目录）
$files = Get-ChildItem -Path $source -File

if ($files.Count -eq 0) {
    Write-Host "[提示] 源目录下没有文件可转移。" -ForegroundColor Yellow
    exit 0
}

Write-Host "[开始] 共找到 $($files.Count) 个文件，正在转移到: $destination" -ForegroundColor Green

$successCount = 0
$failCount = 0

foreach ($file in $files) {
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
