ARG SDK_VERSION=latest
ARG RUNTIME_VERSION=latest

FROM mcr.microsoft.com/dotnet/core/aspnet:${RUNTIME_VERSION} AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:${SDK_VERSION} AS build
WORKDIR /src
COPY . .
RUN dotnet restore UserApi
RUN dotnet build UserApi -c Release -o /app/build

FROM build AS publish
RUN dotnet publish UserApi -c Release -o /app/publish

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserApi.dll"]