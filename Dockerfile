FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY vbetool.csproj ./
RUN dotnet restore vbetool.csproj

COPY . ./
RUN dotnet publish vbetool.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY surusiuotos ./surusiuotos
COPY atsakymai ./atsakymai

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV VbeConfig__BasePath=/app

EXPOSE 10000

ENTRYPOINT ["dotnet", "vbetool.dll"]
