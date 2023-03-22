# User Api Service

## Restore Tools

Run the following in a terminal at the root of the repository

``` shell
dotnet tool restore
```

## Branch name git hook will run on pre commit and control the standard for new branch name.

The branch name should start with: feature/VIH-XXXX-branchName  (X - is digit).
If git version is less than 2.9 the pre-commit file from the .githooks folder need copy to local .git/hooks folder.
To change git hooks directory to directory under source control run (works only for git version 2.9 or greater) :
$ git config core.hooksPath .githooks

## Commit message

The commit message will be validated by prepare-commit-msg hook.
The commit message format should start with : 'feature/VIH-XXXX : ' folowing by 8 or more characters description of commit, otherwise the warning message will be presented.

## Run Zap scan locally

To run Zap scan locally update the following settings and run acceptance\integration tests

User Secrets:

- "VhServices:UserApiUrl": "https://userapi_ac/"

Update following configuration under appsettings.json under UserApi.AcceptanceTests or  UserApi.IntegrationTests

- "VhServices:UserApiUrl": "https://userapi_ac/"
- "ZapConfiguration:ZapScan": true

Note: Ensure you have Docker desktop engine installed and setup

## Run Stryker

To run stryker mutation test, go to UnitTest folder under command prompt and run the following command

```bash
dotnet stryker
```

From the results look for line(s) of code highlighted with Survived\No Coverage and fix them.
