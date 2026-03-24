@echo off
REM Quick script to run contract diagnostics
echo.
echo ================================================================
echo   Running ProjectX Contract Search Diagnostics
echo ================================================================
echo.

REM Check if credentials are set
if "%PROJECTX_API_KEY%"=="" (
    if "%PROJECTX_API_SECRET%"=="" (
        echo WARNING: Environment variables not set
        echo.
        echo The tool will attempt to use credentials from:
        echo   - MarqSpec.Client.ProjectX.Tests\appsettings.integration.json
        echo   - MarqSpec.Client.ProjectX.Diagnostics\appsettings.json
        echo.
    )
)

cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run --configuration Release

echo.
pause
