#!/bin/bash
REMOTE_SERVER="user@example.com"
. deploy.sh.user
# Exit on any error
set -e

# Keep track of the last executed command
trap 'last_command=$current_command; current_command=$BASH_COMMAND' DEBUG

# Echo an error message before exiting
trap 'exit_code=$?; if [ $exit_code -ne 0 ]; then echo "The command \"${last_command}\" failed with exit code $exit_code."; fi' EXIT
dotnet clean
dotnet build /p:Configuration=Release 
dotnet publish -c Release -o bin
# Build the Docker image
docker compose build
docker save -o bin/ipvcr-web.img ipvcr-web
# Check if the remote server is reachable

if ! ssh -o BatchMode=yes -o ConnectTimeout=5 "$REMOTE_SERVER" exit; then
    echo "Remote server is not reachable. Exiting."
    exit 1
fi
scp bin/ipvcr-web.img $REMOTE_SERVER:~ 
ssh $REMOTE_SERVER << 'ENDSSH'

# remove the running container
echo "Removing the running container..."
docker rm -f ipvcr-web

# remove the existing image
echo "Removing the existing image..."
docker rmi ipvcr-web

# load the new image
echo "Loading the new image..."
docker load -i ipvcr-web.img

# deploy the new image to a new container
echo "Deploying the new image to a new container..."
docker run --name ipvcr-web --restart unless-stopped --network host -d -v /media/series:/media -v /var/lib/iptvscheduler:/data ipvcr-web:latest

# remove the image from the remote server
echo "Removing the image from the remote server..."
rm ipvcr-web.img

echo "Deployment completed successfully."
# Check if the container is running
if [ "$(docker ps -q -f name=ipvcr-web)" ]; then
    echo "Container is running."
else
    echo "Container is NOT running."
fi
ENDSSH
