#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs ProjectX API Client integration tests
.DESCRIPTION
    Temporarily removes Skip attributes from integration tests and runs them with real API credentials
.EXAMPLE
    .\run-integration-tests.ps1 -TestCategory All
.EXAMPLE
    .\run-integration-tests.ps1 -TestCategory Authentication
.EXAMPLE
    .\run-integration-tests.ps1 -TestCategory MarketData
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('All', 'Authentication', 'MarketData')]
    [string]$TestCategory = 'All',
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

# Color output functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Failure { param($Message) Write-Host $Message -ForegroundColor Red }

Write-Info "========================================"
Write-Info "ProjectX Integration Test Runner"
Write-Info "========================================"

# Check for credentials
Write-Info "`nChecking API credentials..."
$apiKey = $env:PROJECTX_API_KEY
$apiSecret = $env:PROJECTX_API_SECRET

if (-not $apiKey -or -not $apiSecret) {
    Write-Failure "`n✗ API credentials not found!"
    Write-Warning "`nPlease set the following environment variables:"
    Write-Host "  `$env:PROJECTX_API_KEY = `"your-api-key`""
    Write-Host "  `$env:PROJECTX_API_SECRET = `"your-api-secret`""
    Write-Warning "`nThen run this script again."
    exit 1
}

Write-Success "✓ API Key found ($($apiKey.Length) characters)"
Write-Success "✓ API Secret found ($($apiSecret.Length) characters)"

# Define test files
$authTestFile = "MarqSpec.Client.ProjectX.Tests\Integration\AuthenticationIntegrationTests.cs"
$marketDataTestFile = "MarqSpec.Client.ProjectX.Tests\Integration\MarketDataIntegrationTests.cs"
$backupSuffix = ".backup.$(Get-Date -Format 'yyyyMMddHHmmss')"

# Files to modify
$filesToModify = @()
switch ($TestCategory) {
    'All' { 
        $filesToModify = @($authTestFile, $marketDataTestFile)
        Write-Info "`nTest Category: All (Authentication + Market Data)"
    }
    'Authentication' { 
        $filesToModify = @($authTestFile)
        Write-Info "`nTest Category: Authentication only"
    }
    'MarketData' { 
        $filesToModify = @($marketDataTestFile)
        Write-Info "`nTest Category: Market Data only"
    }
}

# Backup and modify test files
Write-Info "`nPreparing test files..."
$backupFiles = @()

try {
    foreach ($file in $filesToModify) {
        if (Test-Path $file) {
            $backupPath = "$file$backupSuffix"
            Copy-Item $file $backupPath
            $backupFiles += $backupPath
            Write-Success "  ✓ Backed up: $(Split-Path $file -Leaf)"
            
            # Remove Skip attributes
            $content = Get-Content $file -Raw
            $modifiedContent = $content -replace '\[Fact\(Skip = "[^"]+"\)\]', '[Fact]'
            Set-Content $file $modifiedContent -NoNewline
            Write-Success "  ✓ Enabled tests: $(Split-Path $file -Leaf)"
        }
        else {
            Write-Warning "  ! File not found: $file"
        }
    }

    # Build if needed
    if (-not $SkipBuild) {
        Write-Info "`nBuilding solution..."
        $buildResult = dotnet build --verbosity quiet
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✓ Build successful"
        }
        else {
            Write-Failure "✗ Build failed"
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }

    # Run tests
    Write-Info "`nRunning integration tests..."
    Write-Info "This may take several minutes as tests make real API calls.`n"
    Write-Host "----------------------------------------`n" -ForegroundColor DarkGray

    $filter = switch ($TestCategory) {
        'All' { "FullyQualifiedName~IntegrationTests" }
        'Authentication' { "FullyQualifiedName~AuthenticationIntegrationTests" }
        'MarketData' { "FullyQualifiedName~MarketDataIntegrationTests" }
    }

    dotnet test --no-build --filter $filter --verbosity normal

    $testExitCode = $LASTEXITCODE
    Write-Host "`n----------------------------------------" -ForegroundColor DarkGray

    if ($testExitCode -eq 0) {
        Write-Success "`n✓ All tests passed!"
    }
    else {
        Write-Failure "`n✗ Some tests failed (exit code: $testExitCode)"
    }
}
catch {
    Write-Failure "`n✗ Error occurred: $_"
    $testExitCode = 1
}
finally {
    # Restore original files
    Write-Info "`nRestoring original test files..."
    foreach ($backupFile in $backupFiles) {
        $originalFile = $backupFile -replace [regex]::Escape($backupSuffix), ''
        if (Test-Path $backupFile) {
            Move-Item $backupFile $originalFile -Force
            Write-Success "  ✓ Restored: $(Split-Path $originalFile -Leaf)"
        }
    }
}

Write-Info "`n========================================"
if ($testExitCode -eq 0) {
    Write-Success "Integration Tests: SUCCESS"
} else {
    Write-Failure "Integration Tests: FAILED"
}
Write-Info "========================================`n"

exit $testExitCode
