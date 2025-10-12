
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY . .

RUN dotnet restore "Telegram Analytic/Telegram Analytic.csproj"
RUN dotnet publish "Telegram Analytic/Telegram Analytic.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Telegram Analytic.dll"]
