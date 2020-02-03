FROM alpine as unzip
WORKDIR /tmp
RUN apk update && apk add --no-cache unzip
ADD us13server3997.zip .
RUN unzip us13server3997 -d server

FROM ubuntu
EXPOSE 7777
EXPOSE 7778
WORKDIR /server
RUN apt-get update && apt-get install -y \
    ca-certificates \
    jq \
    moreutils
COPY --from=unzip /tmp/server .
RUN chmod +x Unitystation-Server
COPY entrypoint.sh .
CMD /server/entrypoint.sh