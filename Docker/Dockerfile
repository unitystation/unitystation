FROM alpine as unzip
WORKDIR /tmp
RUN apk update && apk add --no-cache unzip
ADD StandaloneLinux64.zip .
RUN unzip StandaloneLinux64 -d server

FROM debian
EXPOSE 7777
EXPOSE 7778
WORKDIR /server
COPY --from=unzip /tmp/server/StandaloneLinux64/UnityStation .
RUN chmod +x UnityStation
CMD /server/UnityStation -batchmode -nographics -logfile /dev/stdout