ARG KAFKA_BROKER=localhost:29092
ARG REDIS_HOST=localhost
ARG NEW_RELIC_LICENSE_KEY

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
ARG KAFKA_BROKER
ENV KAFKA_BROKER=$KAFKA_BROKER
ARG REDIS_HOST
ENV REDIS_HOST=$REDIS_HOST

# New Relic agent install and configuration
ENV NEW_RELIC_LICENSE_KEY = ${NEW_RELIC_LICENSE_KEY}

## Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y newrelic-dotnet-agent

## Enable the agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=NEW_RELIC_LICENSE_KEY \
NEW_RELIC_APP_NAME=raw-input-service

# Copy the built app to the container
WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
ENTRYPOINT ["dotnet", "raw-input-service.dll"]