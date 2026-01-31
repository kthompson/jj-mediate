#!/usr/bin/env pwsh
# Run all tests

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'normal'
)

Write-Host "Running tests ($Configuration)..." -ForegroundColor Cyan
dotnet test --configuration $Configuration --verbosity $Verbosity

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All tests passed" -ForegroundColor Green
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
