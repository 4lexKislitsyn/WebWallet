#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["WebWallet.API/WebWallet.API.csproj", "WebWallet.API/"]
RUN dotnet restore "WebWallet.API/WebWallet.API.csproj"
COPY . .
WORKDIR "/src/WebWallet.API"
RUN dotnet build "WebWallet.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebWallet.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebWallet.API.dll"]