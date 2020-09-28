#!/bin/bash
FROM node
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin \
&& /root/.dotnet/dotnet tool install --global PowerShell
#export PATH=/root/.dotnet:/root/.dotnet/tools:$PATH
ENV PATH="/root/.dotnet:/root/.dotnet/tools:${PATH}"
#export DOTNET_ROOT=$(dirname $(realpath $(which dotnet)))
ENV DOTNET_ROOT="/root/.dotnet"
SHELL ["pwsh"]
ENTRYPOINT ["pwsh"] 