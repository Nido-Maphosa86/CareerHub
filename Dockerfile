# Stage 1 — build
# The SDK image has the full .NET toolchain (compiler, NuGet, etc).
# This stage is not part of the final image — it only produces the
# published output, which keeps the final image small.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first and restore.
# Docker caches layers — as long as the .csproj doesn't change, this
# layer is reused on the next build instead of re-downloading every
# NuGet package from scratch.
COPY CareerHub.Api/CareerHub.Api.csproj CareerHub.Api/
RUN dotnet restore CareerHub.Api/CareerHub.Api.csproj

# Now copy the rest of the source and publish.
COPY CareerHub.Api/ CareerHub.Api/
RUN dotnet publish CareerHub.Api/CareerHub.Api.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2 — runtime (much smaller image)
# The aspnet runtime image has only what is needed to run an already
# built ASP.NET Core app — no SDK, no compiler.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core 8+ images listen on 8080 by default inside the container.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CareerHub.Api.dll"]
