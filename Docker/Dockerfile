FROM ubuntu
RUN apt-get update && apt-get install -y \
    ca-certificates \
    jq \
    moreutils
WORKDIR /server
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh
COPY server .
RUN chmod +x Unitystation
CMD /server/entrypoint.sh
EXPOSE 7777
EXPOSE 7778