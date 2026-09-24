FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/BibliotecaEscolar.Api/BibliotecaEscolar.Api.csproj", "src/BibliotecaEscolar.Api/"]

RUN dotnet restore "src/BibliotecaEscolar.Api/BibliotecaEscolar.Api.csproj"

COPY . .

WORKDIR "/src/src/BibliotecaEscolar.Api"

RUN dotnet publish "BibliotecaEscolar.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BibliotecaEscolar.Api.dll"]