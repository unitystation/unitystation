FROM ubuntu
RUN apt-get update && apt-get install -y \
    ca-certificates \
    jq \
    moreutils
WORKDIR /server
COPY entrypoint.sh .
COPY server .
RUN chmod +x UnityStation
RUN mkdir UnityStation_Data/StreamingAssets/config
RUN echo '{"RconPort": 7778}' > UnityStation_Data/StreamingAssets/config/config.json
CMD /server/entrypoint.sh
EXPOSE 7777
EXPOSE 7778