#!/bin/bash

# Navigate to the React app directory
cd "$(dirname "$0")/ipvcr.Frontend"

# Install dependencies if needed
npm install

# Create the target directory in the ASP.NET Core project
mkdir -p ../ipvcr.Web/wwwroot

# Remove existing React app files from wwwroot
rm -rf ../ipvcr.Web/wwwroot/*

# Build the React app
npm run build && \
 cp -r build/* ../ipvcr.Web/wwwroot/ && \
 echo "React app built and copied to ipvcr.Web/wwwroot successfully"
