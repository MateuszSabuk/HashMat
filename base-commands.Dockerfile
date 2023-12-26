FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install the additional packages
RUN apt-get update && \
    apt-get install -y gcc build-essential git zlib1g-dev libssl-dev wget p7zip && \
    cd /opt && \
    git clone https://github.com/openwall/john.git && \
    cd john/src && \
    ./configure && \
    make && \
    make install && \
    cd /opt && \
    wget https://hashcat.net/files/hashcat-6.2.6.7z && \
    p7zip -d hashcat* && \
    mv hashcat* hashcat && \
    mkdir wordlists && \
    cd wordlists && \
    wget http://downloads.skullsecurity.org/passwords/rockyou.txt.bz2 && \
    bunzip2 rockyou.txt.bz2 && \
    rm -rf /var/lib/apt/lists/*
