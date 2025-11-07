FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY LongJob/LongJob.csproj LongJob/
COPY LongJob.Application/LongJob.Application.csproj LongJob.Application/
COPY LongJob.Domain/LongJob.Domain.csproj LongJob.Domain/
COPY LongJob.Infrastructure/LongJob.Infrastructure.csproj LongJob.Infrastructure/
RUN dotnet restore LongJob/LongJob.csproj
COPY . .
WORKDIR /src/LongJob
RUN dotnet build LongJob.csproj -c Release -o /app/build

FROM build AS publish
WORKDIR /src/LongJob
RUN dotnet publish LongJob.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LongJob.dll"]
