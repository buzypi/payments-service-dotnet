FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PaymentsService.csproj", "./"]
RUN dotnet restore "PaymentsService.csproj"
COPY . .
RUN dotnet build "PaymentsService.csproj" -c Release -o /app/build
RUN dotnet publish "PaymentsService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentsService.dll"]
