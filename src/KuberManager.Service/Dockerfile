FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001
USER app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/KuberManager.Contracts/KuberManager.Contracts.csproj", "src/KuberManager.Contracts/"]
COPY ["src/KuberManager.Service/KuberManager.Service.csproj", "src/KuberManager.Service/"]
RUN dotnet restore "src/KuberManager.Service/KuberManager.Service.csproj"
COPY . .
RUN dotnet build "src/KuberManager.Service/KuberManager.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/KuberManager.Service/KuberManager.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KuberManager.Service.dll"]
