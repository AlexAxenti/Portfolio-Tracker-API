FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PortfolioTracker.Api/PortfolioTracker.Api.csproj", "PortfolioTracker.Api/"]
RUN dotnet restore "PortfolioTracker.Api/PortfolioTracker.Api.csproj"

COPY . .
WORKDIR "/src/PortfolioTracker.Api"
RUN dotnet publish "PortfolioTracker.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PortfolioTracker.Api.dll"]