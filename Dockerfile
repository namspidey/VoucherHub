FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/ backend/src/
RUN dotnet restore backend/src/VoucherHub.Api/VoucherHub.Api.csproj
RUN dotnet publish backend/src/VoucherHub.Api/VoucherHub.Api.csproj -c Release \
    --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VoucherHub.Api.dll"]
