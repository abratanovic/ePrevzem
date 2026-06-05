FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ePrevzem.sln .
COPY backend/ePrevzem.Api/ePrevzem.Api.csproj backend/ePrevzem.Api/
COPY backend/ePrevzem.Application/ePrevzem.Application.csproj backend/ePrevzem.Application/
COPY backend/ePrevzem.Domain/ePrevzem.Domain.csproj backend/ePrevzem.Domain/
COPY backend/ePrevzem.Infrastructure/ePrevzem.Infrastructure.csproj backend/ePrevzem.Infrastructure/
COPY backend/ePrevzem.Tests/ePrevzem.Tests.csproj backend/ePrevzem.Tests/
RUN dotnet restore backend/ePrevzem.Api/ePrevzem.Api.csproj
COPY . .
RUN dotnet publish backend/ePrevzem.Api/ePrevzem.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ePrevzem.Api.dll"]
