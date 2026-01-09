Write-Host "Удаление папки Build..." -ForegroundColor Cyan

$buildPath = Join-Path -Path (Get-Location).Path -ChildPath "..\Build"

if (Test-Path -Path $buildPath) {
    try {
        Remove-Item -Path $buildPath -Recurse -Force
        Write-Host "Папка Build успешно удалена." -ForegroundColor Green
    } catch {
        Write-Host "Ошибка при удалении папки Build: $_" -ForegroundColor Red
    }
} else {
    Write-Host "Папка Build не найдена." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Нажмите любую клавишу для выхода..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 