FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CardStudio.Api/CardStudio.Api.csproj", "src/CardStudio.Api/"]
COPY ["src/CardStudio.Shared/CardStudio.Shared.csproj", "src/CardStudio.Shared/"]
COPY ["src/CardStudio.ServiceDefaults/CardStudio.ServiceDefaults.csproj", "src/CardStudio.ServiceDefaults/"]
RUN dotnet restore "src/CardStudio.Api/CardStudio.Api.csproj"
COPY . .
WORKDIR /src/src/CardStudio.Api
RUN dotnet build "CardStudio.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CardStudio.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CardStudio.Api.dll"]
