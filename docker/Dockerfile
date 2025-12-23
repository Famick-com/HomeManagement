FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Famick.HomeManagement.Web/Famick.HomeManagement.Web.csproj", "src/Famick.HomeManagement.Web/"]
RUN dotnet restore "src/Famick.HomeManagement.Web/Famick.HomeManagement.Web.csproj"
COPY . .
WORKDIR "/src/src/Famick.HomeManagement.Web"
RUN dotnet build "Famick.HomeManagement.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Famick.HomeManagement.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Famick.HomeManagement.Web.dll"]
