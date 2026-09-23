# 하나의 Dockerfile 로 모든 서비스를 빌드한다.
#   docker build --build-arg PROJECT=src/GamePortal.Web.Api -t gameportal-web-api .
ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG PROJECT
WORKDIR /repo

# 1) 패키지 복원 레이어 캐시: 프로젝트 파일이 바뀌지 않으면 restore 를 건너뛴다
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/GamePortal.Domain/*.csproj src/GamePortal.Domain/
COPY src/GamePortal.Application/*.csproj src/GamePortal.Application/
COPY src/GamePortal.Infrastructure/*.csproj src/GamePortal.Infrastructure/
COPY src/GamePortal.AspNetCore/*.csproj src/GamePortal.AspNetCore/
COPY src/GamePortal.Web.Api/*.csproj src/GamePortal.Web.Api/
COPY src/GamePortal.Admin.Api/*.csproj src/GamePortal.Admin.Api/
COPY src/GamePortal.Web/*.csproj src/GamePortal.Web/
COPY src/GamePortal.Worker/*.csproj src/GamePortal.Worker/
COPY tools/GameServer.Mock/*.csproj tools/GameServer.Mock/
RUN dotnet restore "${PROJECT}"

# 2) 소스 복사 후 publish
COPY src/ src/
COPY tools/ tools/
RUN dotnet publish "${PROJECT}" -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
ARG PROJECT
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=1
COPY --from=build /app .
# 어셈블리 이름 = 프로젝트 폴더명 (PROJECT 인자로 결정되므로 런타임 환경변수로 전달)
ENV APP_DLL=${PROJECT}
# .NET 8 이미지 기본 제공 non-root 사용자
USER app
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet \"$(basename \"$APP_DLL\").dll\""]
