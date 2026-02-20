FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SimpleOrders.DumbRabbit/SimpleOrders.DumbRabbit.csproj", "SimpleOrders.DumbRabbit/"]
COPY ["SimpleOrders.Shared/SimpleOrders.Shared.csproj", "SimpleOrders.Shared/"]
RUN dotnet restore "SimpleOrders.DumbRabbit/SimpleOrders.DumbRabbit.csproj"
COPY . .
WORKDIR "/src/SimpleOrders.DumbRabbit"
RUN dotnet build "./SimpleOrders.DumbRabbit.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SimpleOrders.DumbRabbit.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleOrders.DumbRabbit.dll"]
