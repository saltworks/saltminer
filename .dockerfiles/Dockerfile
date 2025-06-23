FROM mcr.microsoft.com/dotnet/aspnet:8.0.17-alpine3.22-amd64

LABEL base-image="mcr.microsoft.com/dotnet/aspnet:8.0.17-alpine3.22-amd64"
LABEL vendor="Saltworks Security, LLC"

ARG ARTIFACT_PATH=.
ARG smapp=/usr/share/saltworks
ARG smapp3=${smapp}/saltminer-3.0.0/api
ARG smcfg=/etc/saltworks
ARG smcfg3=${smcfg}/saltminer-3.0.0/api
ARG smlog=/var/log/saltworks
ARG smlog3=${smlog}/saltminer-3.0.0

ENV ARTIFACT_PATH=$ARTIFACT_PATH
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV SALTMINER_API_CONFIG_PATH=/etc/saltworks/saltminer-3.0.0/api

WORKDIR ${smapp3}

COPY $ARTIFACT_PATH .

# curl is used for healthcheck.
# jq is used for configuration file manipulation
RUN apk add --no-cache curl jq bash \
  && mkdir -p ${smcfg3} \
  && mkdir -p ${smlog3} \
  && chmod u+x config-api.sh \
  && mv ${smapp3}/appsettings.json ${smcfg3}/
COPY $ARTIFACT_PATH/data-templates/ ./data

EXPOSE 5000

ENTRYPOINT ["/usr/bin/dotnet", "/usr/share/saltworks/saltminer-3.0.0/api/Saltworks.SaltMiner.DataApi.dll"]
