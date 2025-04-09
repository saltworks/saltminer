FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.21-amd64

ARG ARTIFACT_PATH=.
ARG smapp=/usr/share/saltworks
ARG smapp3=${smapp}/saltminer-3.0.0
ARG smcfg=/etc/saltworks
ARG smcfg3=${smcfg}/saltminer-3.0.0
ARG smlog=/var/log/saltworks
ARG smlog3=${smlog}/saltminer-3.0.0
ENV ARTIFACT_PATH=$ARTIFACT_PATH

# curl is used for healthcheck.
RUN apk add --no-cache curl

RUN mkdir -p ${smcfg3}/api
RUN mkdir -p ${smlog3}

WORKDIR /usr/share/saltworks/saltminer-3.0.0/api

COPY $ARTIFACT_PATH .
RUN mv ${smapp3}/api/appsettings.json ${smcfg3}/api/

COPY $ARTIFACT_PATH/data-templates/ ./data

ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false
ENV SALTMINER_API_CONFIG_PATH=/etc/saltworks/saltminer-3.0.0/api

EXPOSE 5000

ENTRYPOINT ["/usr/bin/dotnet", "/usr/share/saltworks/saltminer-3.0.0/api/Saltworks.SaltMiner.DataApi.dll"]
