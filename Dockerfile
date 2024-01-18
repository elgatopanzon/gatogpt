FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Copy code into /code
COPY . ./code

WORKDIR /code
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Debug -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install dependencies
RUN apt update -y
RUN apt install -y build-essential cmake gcc software-properties-common wget
RUN apt install -y git openssh-client

# Download nvidia cuda toolkit
RUN wget --quiet https://developer.download.nvidia.com/compute/cuda/12.3.2/local_installers/cuda-repo-debian12-12-3-local_12.3.2-545.23.08-1_amd64.deb

# Copy everything and clone required project libraries
RUN mkdir /code

RUN git clone https://github.com/elgatopanzon/godotegp-framework.git /code/libs/godotegp-framework
RUN git clone https://github.com/elgatopanzon/godotegp-framework-nongame.git /code/libs/godotegp-framework-nongame

# Install nvidia cuda toolkit
RUN dpkg -i cuda-repo-debian12-12-3-local_12.3.2-545.23.08-1_amd64.deb \
	&& cp /var/cuda-repo-debian12-12-3-local/cuda-*-keyring.gpg /usr/share/keyrings/ \
	&& add-apt-repository contrib \
	&& apt-get update \
	&& apt-get -y install cuda-toolkit-12-3

# Compile llama.cpp
RUN git clone https://github.com/ggerganov/llama.cpp \
	&& cd llama.cpp \
	&& mkdir build \
	&& cd build \
	&& CUDACXX=/usr/local/cuda-12/bin/nvcc cmake .. -DLLAMA_CUBLAS=ON -DCMAKE_CUDA_ARCHITECTURES=all \
	&& CUDACXX=/usr/local/cuda-12/bin/nvcc cmake --build . --config Release

# Move required binaries
RUN mv llama.cpp/build/bin/main /code/llama.cpp
RUN mv llama.cpp/build/bin/llava-cli /code/llama.cpp-llava-cli
RUN mv llama.cpp/build/bin/server /code/llama.cpp-server

# Create running user
ARG UNAME=user
ARG UID=1000
ARG GID=1000
RUN groupadd -g $GID -o $UNAME
RUN useradd -m -u $UID -g $GID -o -s /bin/bash $UNAME

# Copy deployment
COPY --from=build-env /code/out /code
COPY --from=build-env /code/out /code/bin/Debug/net8.0
WORKDIR /code

RUN chown -R $UNAME:$UNAME /code

# Switch to running user
USER $UNAME

# Release build not working, for now issue debug run command
ENTRYPOINT ["dotnet", "/code/bin/Debug/net8.0/GatoGPT.dll"]
