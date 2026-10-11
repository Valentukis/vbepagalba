FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY vbesort/VbeSort.csproj vbesort/
RUN dotnet restore vbesort/VbeSort.csproj

COPY vbesort/ vbesort/
RUN dotnet publish vbesort/VbeSort.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY surusiuotos ./surusiuotos
COPY atsakymai ./atsakymai

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV VbeConfig__BasePath=/app

EXPOSE 10000

ENTRYPOINT ["dotnet", "VbeSort.dll"]
