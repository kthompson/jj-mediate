#!/usr/bin/env pwsh
# Build the project

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Write-Host "Building project ($Configuration)..." -ForegroundColor Cyan
dotnet build --configuration $Configuration

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
