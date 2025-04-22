FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install required packages and Node.js for frontend build
RUN apt-get update && \
    apt-get install -y at ffmpeg curl && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /source

# Copy the solution file and restore dependencies
COPY ["ipvcr.sln", "."]
COPY ["ipvcr.Logic/ipvcr.Logic.csproj", "ipvcr.Logic/"]
COPY ["ipvcr.Tests/ipvcr.Tests.csproj", "ipvcr.Tests/"]
COPY ["ipvcr.Web/ipvcr.Web.csproj", "ipvcr.Web/"]

RUN dotnet restore "ipvcr.sln"

# Copy frontend package.json and install dependencies
COPY ["ipvcr.Frontend/package.json", "ipvcr.Frontend/package-lock.json*", "ipvcr.Frontend/"]
WORKDIR /source/ipvcr.Frontend
RUN npm install

# Copy all source code
WORKDIR /source
COPY . .

# Build the frontend
WORKDIR /source/ipvcr.Frontend
RUN npm run build

# Create wwwroot directory and copy the React build output
RUN mkdir -p /source/ipvcr.Web/wwwroot
RUN cp -r /source/ipvcr.Frontend/build/* /source/ipvcr.Web/wwwroot/

# Build the .NET solution
WORKDIR /source
RUN dotnet build "ipvcr.sln" -c Release -o /app/build
RUN dotnet publish "ipvcr.Web/ipvcr.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# # OPTIONAL: Set timezone
# ENV TZ=Europe/Amsterdam
# RUN apt-get update && \
#     apt-get install -y tzdata && \
#     ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
#     echo $TZ > /etc/timezone

# Install required packages in final image
RUN apt-get update && \
    apt-get install -y at ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create required directories
RUN mkdir -p /var/spool/cron/atjobs && \
    mkdir -p /media && \
    mkdir -p /data && \
    mkdir -p /etc/iptvscheduler && \
    chmod -R 755 /var/spool/cron/atjobs /media /data /etc/iptvscheduler

COPY --from=build /app/publish .

CMD ["bash", "-c", "/usr/sbin/atd && dotnet ipvcr.Web.dll"]

