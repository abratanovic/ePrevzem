FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY sitrust-mock.sln .
COPY backend/SiTrustMock/SiTrustMock.csproj backend/SiTrustMock/
COPY backend/SiTrustMock.Tests/SiTrustMock.Tests.csproj backend/SiTrustMock.Tests/
RUN dotnet restore backend/SiTrustMock/SiTrustMock.csproj
COPY . .
RUN dotnet publish backend/SiTrustMock/SiTrustMock.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SiTrustMock.dll"]
