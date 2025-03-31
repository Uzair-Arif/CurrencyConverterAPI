FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CurrencyConverterAPI/CurrencyConverterAPI.csproj", "CurrencyConverterAPI/"]
RUN dotnet restore "CurrencyConverterAPI/CurrencyConverterAPI.csproj"

COPY . .
WORKDIR "/src/CurrencyConverterAPI"
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CurrencyConverterAPI.dll"]