#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 9100
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["aag_exporter/aag_exporter.csproj", "aag_exporter/"]
RUN dotnet restore "aag_exporter/aag_exporter.csproj"
COPY . .
WORKDIR "/src/aag_exporter"
RUN dotnet build "aag_exporter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "aag_exporter.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "aag_exporter.dll"]