FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

WORKDIR /mod

COPY src/*.csproj .

RUN dotnet restore

COPY . .

RUN dotnet build -c Release -o release

RUN rm /mod/release/CounterStrikeSharp.API.dll

FROM debian:bookworm-slim

ENV DATA_DIR="/serverdata"
ENV STEAMCMD_DIR="${DATA_DIR}/steamcmd"
ENV BASE_SERVER_DIR="${DATA_DIR}/serverfiles"
ENV INSTANCE_SERVER_DIR="/opt/instance"

ENV GAME_ID="730"
ENV GAME_NAME="counter-strike"
ENV GAME_PARAMS=""
ENV GAME_PORT=27015
ENV VALIDATE=false
ENV UMASK=000
ENV UID=99
ENV GID=100
ENV USERNAME=""
ENV PASSWRD=""
ENV USER="steam"
ENV DATA_PERM=770

ENV SERVER_ID=""

ENV METAMOD_URL=https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git1313-linux.tar.gz
ENV COUNTER_STRIKE_SHARP_URL=https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v253/counterstrikesharp-with-runtime-build-253-linux-5644921.zip

RUN  echo "deb http://deb.debian.org/debian bookworm contrib non-free non-free-firmware" >> /etc/apt/sources.list && \
	apt-get update && apt-get -y upgrade && \
	apt-get -y install --no-install-recommends wget locales procps && \
	touch /etc/locale.gen && \
	echo "en_US.UTF-8 UTF-8" >> /etc/locale.gen && \
	locale-gen && \
	apt-get -y install --reinstall ca-certificates && \
	rm -rf /var/lib/apt/lists/*

ENV LANG=en_US.UTF-8
ENV LANGUAGE=en_US:en
ENV LC_ALL=en_US.UTF-8

RUN apt-get update && \
	apt-get -y install --no-install-recommends curl unzip lib32gcc-s1 lib32stdc++6 lib32z1 lsof libicu-dev && \
	rm -rf /var/lib/apt/lists/*

RUN mkdir $DATA_DIR && \
	mkdir $STEAMCMD_DIR && \
	mkdir $BASE_SERVER_DIR && \
    mkdir $INSTANCE_SERVER_DIR && \
	useradd -d $DATA_DIR -s /bin/bash $USER && \
	ulimit -n 2048

RUN mkdir /opt/metamod
ADD $METAMOD_URL /tmp/metamod.tar.gz
RUN tar -xz -C /opt/metamod -f /tmp/metamod.tar.gz && rm /tmp/metamod.tar.gz

RUN mkdir /opt/counterstrikesharp
ADD $COUNTER_STRIKE_SHARP_URL /tmp/counterstrikesharp.zip
RUN unzip /tmp/counterstrikesharp.zip -d /opt/counterstrikesharp && rm /tmp/counterstrikesharp.zip

COPY /cfg /opt/server-cfg
COPY /scripts /opt/scripts
COPY --from=build /mod/release /opt/mod

RUN mv /opt/metamod/addons /opt/addons

RUN cp -R /opt/counterstrikesharp/addons/metamod /opt/addons
RUN cp -R /opt/counterstrikesharp/addons/counterstrikesharp /opt/addons
RUN mkdir -p /opt/addons/counterstrikesharp/plugins

RUN rm -rf /opt/metamod
RUN rm -rf /opt/counterstrikesharp

#Server Start
ENTRYPOINT ["/opt/scripts/server.sh"]