#!/usr/bin/env pwsh
# Format all code with CSharpier

Write-Host "Formatting code..." -ForegroundColor Cyan
dotnet csharpier format .

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Code formatted successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Formatting failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
