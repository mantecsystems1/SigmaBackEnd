FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["sigmaBack.sln", "."]
COPY ["sigmaBack.API/sigmaBack.API.csproj", "sigmaBack.API/"]
COPY ["sigmaBack.Application/sigmaBack.Application.csproj", "sigmaBack.Application/"]
COPY ["sigmaBack.Domain/sigmaBack.Domain.csproj", "sigmaBack.Domain/"]
COPY ["sigmaback.Infra.Data/sigmaback.Infra.Data.csproj", "sigmaback.Infra.Data/"]

RUN dotnet restore

COPY . .
WORKDIR "/src/sigmaBack.API"

RUN dotnet publish "sigmaBack.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "sigmaBack.API.dll"]