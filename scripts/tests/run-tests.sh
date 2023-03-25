#!/bin/sh
set -x

rm -d -r ${PWD}/Coverage
rm -d -r ${PWD}/TestResults

exclusions="[Testing.Common]*,[UserApi.*Tests?]*,[UserApi.API]Startup,[UserApi.Common]*"
configuration=Release

dotnet build UserApi/UserApi.sln -c $configuration
# Script is for docker compose tests where the script is at the root level
dotnet test UserApi/UserApi.UnitTests/UserApi.UnitTests.csproj -c $configuration --no-build --results-directory ./TestResults --logger "trx;LogFileName=UserApi-Unit-Tests-TestResults.trx" \
    "/p:CollectCoverage=true" \
    "/p:Exclude=\"${exclusions}\"" \
    "/p:CoverletOutput=${PWD}/Coverage/" \
    "/p:MergeWith=${PWD}/Coverage/coverage.json" \
    "/p:CoverletOutputFormat=\"opencover,json,cobertura,lcov\""

## Enable this when integration tests are ready
# dotnet test UserApi/UserApi.IntegrationTests/UserApi.IntegrationTests.csproj -c $configuration --no-build --results-directory ./TestResults --logger "trx;LogFileName=UserApi-Integration-Tests-TestResults.trx" \
#     "/p:CollectCoverage=true" \
#     "/p:Exclude=\"${exclusions}\"" \
#     "/p:CoverletOutput=${PWD}/Coverage/" \
#     "/p:MergeWith=${PWD}/Coverage/coverage.json" \
#     "/p:CoverletOutputFormat=\"opencover,json,cobertura,lcov\""
