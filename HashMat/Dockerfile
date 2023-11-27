#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM samatebu/hashmat:base AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HashMat/HashMat.csproj", "HashMat/"]
RUN dotnet restore "HashMat/HashMat.csproj"
COPY . .
WORKDIR "/src/HashMat"
RUN dotnet build "HashMat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HashMat.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HashMat.dll"]