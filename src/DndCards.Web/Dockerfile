FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/DndCards.Web/DndCards.Web.csproj", "src/DndCards.Web/"]
COPY ["src/DndTools.ServiceDefaults/DndTools.ServiceDefaults.csproj", "src/DndTools.ServiceDefaults/"]
RUN dotnet restore "src/DndCards.Web/DndCards.Web.csproj"
COPY . .
WORKDIR /src/src/DndCards.Web
RUN dotnet build "DndCards.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DndCards.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DndCards.Web.dll"]
