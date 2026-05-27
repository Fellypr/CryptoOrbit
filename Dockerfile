FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["CryptoOrbit.csproj", "./"]
RUN dotnet restore "./CryptoOrbit.csproj"


COPY . .


RUN dotnet build "CryptoOrbit.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "CryptoOrbit.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "CryptoOrbit.dll"]
