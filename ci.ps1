#!/usr/bin/env pwsh
# Run full CI pipeline locally: format check, build, and test

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Running CI Pipeline Locally" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Lint
Write-Host "Step 1/3: Checking formatting..." -ForegroundColor Yellow
& ./lint.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Step 2: Build
Write-Host "`nStep 2/3: Building..." -ForegroundColor Yellow
& ./build.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Step 3: Test
Write-Host "`nStep 3/3: Testing..." -ForegroundColor Yellow
& ./test.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "✓ CI Pipeline Passed" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green
