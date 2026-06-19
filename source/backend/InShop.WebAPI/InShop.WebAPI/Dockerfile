FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["InShopDbModels/InShopDbModels.csproj", "InShopDbModels/"]
COPY ["InShopBLLayer/InShopBLLayer.csproj", "InShopBLLayer/"]
COPY ["InShop.WebAPI/InShop.WebAPI.csproj", "InShop.WebAPI/"]
RUN dotnet restore "InShop.WebAPI/InShop.WebAPI.csproj"

COPY . .
RUN dotnet publish "InShop.WebAPI/InShop.WebAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InShop.WebAPI.dll"]