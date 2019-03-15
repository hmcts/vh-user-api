# User Api Service

## Running code coverage

``` bash
dotnet test --no-build UserApi.UnitTests/UserApi.UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat="\"opencover,cobertura,json,lcov\"" /p:CoverletOutput=../Artifacts/Coverage/ /p:MergeWith='../Artifacts/Coverage/coverage.json' /p:Exclude="\"[UserApi.*Tests?]*,[UserApi.API]Startup,[UserApi.Common]*,[Testing.Common]*\""

dotnet test --no-build UserApi.IntegrationTests/UserApi.IntegrationTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat="\"opencover,cobertura,json,lcov\"" /p:CoverletOutput=../Artifacts/Coverage/ /p:MergeWith='../Artifacts/Coverage/coverage.json' /p:Exclude="\"[UserApi.*Tests?]*,[UserApi.API]Startup,[UserApi.Common]*,[Testing.Common]*\""

```

## Generate HTML Report

Under the unit test project directory

``` bash
dotnet reportgenerator "-reports:../Artifacts/Coverage/coverage.opencover.xml" "-targetDir:../Artifacts/Coverage/Report" -reporttypes:HtmlInline_AzurePipelines
```