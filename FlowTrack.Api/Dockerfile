# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FlowTrack.Api/FlowTrack.Api.csproj", "FlowTrack.Api/"]
COPY ["FlowTrack.Application/FlowTrack.Application.csproj", "FlowTrack.Application/"]
COPY ["FlowTrack.Domain/FlowTrack.Domain.csproj", "FlowTrack.Domain/"]
COPY ["FlowTrack.Infrastructure/FlowTrack.Infrastructure.csproj", "FlowTrack.Infrastructure/"]
RUN dotnet restore "FlowTrack.Api/FlowTrack.Api.csproj"

COPY . .
WORKDIR "/src/FlowTrack.Api"
RUN dotnet publish "FlowTrack.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlowTrack.Api.dll"]
