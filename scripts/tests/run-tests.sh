#!/bin/sh
set -x

exclusions="[Testing.Common]*,[UserApi.*Tests?]*,[UserApi.API]Startup,[UserApi.Common]*"
configuration=Release

dotnet build UserApi/UserApi.sln -c $configuration
# Script is for docker compose tests where the script is at the root level
dotnet test UserApi/UserApi.UnitTests/UserApi.UnitTests.csproj -c $configuration --results-directory ./TestResults --logger "trx;LogFileName=UserApi-Unit-Tests-TestResults.trx" \
    "/p:CollectCoverage=true" \
    "/p:Exclude=\"${exclusions}\"" \
    "/p:CoverletOutput=${PWD}/Coverage/" \
    "/p:MergeWith=${PWD}/Coverage/coverage.json" \
    "/p:CoverletOutputFormat=\"opencover,json,cobertura,lcov\""||
{
   echo "##vso[task.logissue type=error]DotNet Unit Tests Failed."
   echo "##vso[task.complete result=Failed]"
   exit 1
}

## Enable this when integration tests are ready
# dotnet test UserApi/UserApi.IntegrationTests/UserApi.IntegrationTests.csproj -c $configuration --results-directory ./TestResults --logger "trx;LogFileName=UserApi-Integration-Tests-TestResults.trx" \
#     "/p:CollectCoverage=true" \
#     "/p:Exclude=\"${exclusions}\"" \
#     "/p:CoverletOutput=${PWD}/Coverage/" \
#     "/p:MergeWith=${PWD}/Coverage/coverage.json" \
#     "/p:CoverletOutputFormat=\"opencover,json,cobertura,lcov\""