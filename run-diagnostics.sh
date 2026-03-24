#!/bin/bash

# Quick script to run contract diagnostics

echo ""
echo "================================================================"
echo "  Running ProjectX Contract Search Diagnostics"
echo "================================================================"
echo ""

# Check if credentials are set
if [ -z "$PROJECTX_API_KEY" ] || [ -z "$PROJECTX_API_SECRET" ]; then
    echo "WARNING: Environment variables not set"
    echo ""
    echo "The tool will attempt to use credentials from:"
    echo "  - MarqSpec.Client.ProjectX.Tests/appsettings.integration.json"
    echo "  - MarqSpec.Client.ProjectX.Diagnostics/appsettings.json"
    echo ""
fi

cd MarqSpec.Client.ProjectX.Diagnostics
dotnet run --configuration Release

echo ""
read -p "Press any key to continue..."
