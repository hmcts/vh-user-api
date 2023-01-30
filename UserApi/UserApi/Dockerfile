# Create Base Image.
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Publish .NET App.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY UserApi .
RUN dotnet restore --configfile nuget.sds.config "UserApi/UserApi.csproj"
WORKDIR "/src/UserApi"
RUN dotnet publish "UserApi.csproj" -c Release -o /app/publish

# Create Final Image.
FROM base AS final
RUN apt-get update && apt-get install -y curl
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserApi.dll"]