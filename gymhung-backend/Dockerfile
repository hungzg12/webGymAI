FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY GymHung.Api/ GymHung.Api/
COPY GymHung.Application/ GymHung.Application/
COPY GymHung.Domain/ GymHung.Domain/
COPY GymHung.Infrastructure/ GymHung.Infrastructure/
RUN dotnet publish GymHung.Api/GymHung.Api.csproj -c Release -o /out --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymHung.Api.dll"]
