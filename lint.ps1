#!/usr/bin/env pwsh
# Check code formatting with CSharpier

Write-Host "Checking code formatting..." -ForegroundColor Cyan
dotnet csharpier check .

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All files are properly formatted" -ForegroundColor Green
} else {
    Write-Host "✗ Some files need formatting. Run ./format.ps1 to fix." -ForegroundColor Red
    exit $LASTEXITCODE
}
