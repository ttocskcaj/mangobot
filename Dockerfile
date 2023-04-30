FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /
COPY ["MangoBot.Infrastructure/MangoBot.Infrastructure.csproj", "MangoBot.Infrastructure/"]
COPY ["MangoBot.WebApp/MangoBot.WebApp.csproj", "MangoBot.WebApp/"]
RUN dotnet restore "MangoBot.WebApp/MangoBot.WebApp.csproj"
COPY . .
WORKDIR "/MangoBot.WebApp"
RUN dotnet build "MangoBot.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MangoBot.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MangoBot.WebApp.dll"]
