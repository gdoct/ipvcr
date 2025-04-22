[![Docker Image CI](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml/badge.svg)](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml)
# IPVCR: A Docker-based IPTV Recorder / Scheduler
![image](https://github.com/user-attachments/assets/7c23f585-7103-4eda-aa75-ba20adfd9c4b)

A web VCR in a docker image. 
This started out as a project so that I could schedule ffmpeg to record some tv. How complex can a simple wrapper for the Linux ```at``` command become? But this has quickly evolved into a semi-overengineered project complete with security, configuration management, full CI/CD, and more planned.

## Features

- Modern React-based web interface with syntax highlighting for task scripts 
- Schedule and manage IPTV recordings with flexible timing options
- Real-time timezone offset handling between user interface and server
- Advanced M3U playlist management with support for very large channel lists (>100 MB)
- Channel search functionality with filtering options
- Secure authentication system with JWT token-based access control
- Task script editing capabilities with syntax highlighting
- Flexible FFmpeg configuration for optimal recording quality
- Robust scheduling through Linux `at` command (Linux) or Task Scheduler (Windows)
- Comprehensive settings management (media paths, data storage, SSL)
- Docker-ready with full containerization support
- Multi-platform support (Linux-first with Windows compatibility)
- RESTful API for programmatic control

## Screenshot

![image](https://github.com/user-attachments/assets/2714a442-5914-46c2-9d52-3755116f6478)

## Requirements
The docker image requires write access to two mounted folders.
 - media : where the recordings are stored
 - data : where it stores its data and settings

The application requires a m3u file with iptv channels. This file can be copied to the mounted data volume, or uploaded through the web interface. The application supports very large m3u files.

## Docker Deployment

To run the docker image on port 5000 using host networking, you can use this command:
```
docker run --name ipvcr --network host -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

To run the docker using bridged networking and redirect the default ports, use this command:
```
docker run --name ipvcr -p 5000:5000 -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

## First run
1. Log on with the default username **admin** and password **ipvcr**
2. Proceed to the settings page (cogwheel in the upper right corner)
3. Upload a m3u file
4. Change the admin password

## Development

This is a .NET project targeting Linux or WSL. To clone and build the solution:

```
git clone https://github.com/gdoct/ipvcr.git
cd ipvcr
./build-frontend.sh
dotnet build
dotnet run --project ipvcr.Web
```

To contribute, just create an issue or a pull request

## Docker Compose

You can also use Docker Compose to run this application:

```
version: '3'
services:
  ipvcr:
    image: ghcr.io/gdoct/ipvcr:latest
    container_name: ipvcr
    ports:
      - "5000:5000"
    volumes:
      - /path/to/media:/media
      - /path/to/data:/data
    restart: unless-stopped
```

Save this to a docker-compose.yml file and run with `docker-compose up -d`.

## Architecture

IPVCR consists of several components:

- **Web UI**: React-based frontend for managing recordings and settings
- **API Layer**: RESTful endpoints for UI interaction 
- **Scheduling Engine**: Manages recording tasks using native OS capabilities
- **Settings Management**: Handles configuration persistence and validation
- **Authentication**: Provides secure access to the application
- **M3U Parser**: Processes IPTV channel lists of various sizes

