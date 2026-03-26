FROM mcr.microsoft.com/dotnet/aspnet:8.0.20-alpine3.22-amd64

LABEL base-image="mcr.microsoft.com/dotnet/aspnet:8.0.20-alpine3.22-amd64"
LABEL vendor="Saltworks Security, LLC"

ARG ARTIFACT_PATH=.
ARG smapp=/opt/saltworks/saltminer/app/api
ARG smcfg=/opt/saltworks/saltminer/config/api
ARG smlog=/opt/saltworks/saltminer/logs

ENV ARTIFACT_PATH=$ARTIFACT_PATH
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV SALTMINER_API_CONFIG_PATH=/opt/saltworks/saltminer/config/api

WORKDIR ${smapp}

COPY $ARTIFACT_PATH .

# curl is used for healthcheck.
# jq is used for configuration file manipulation
RUN apk add --no-cache curl jq bash \
  && mkdir -p ${smcfg} \
  && mkdir -p ${smlog} \
  && chmod u+x config-api.sh \
  && mv ${smapp}/appsettings.json ${smcfg}/
COPY $ARTIFACT_PATH/data-templates/ ./data

EXPOSE 5000

ENTRYPOINT ["/usr/bin/dotnet", "/usr/share/saltworks/saltminer-3.0.0/api/Saltworks.SaltMiner.DataApi.dll"]
