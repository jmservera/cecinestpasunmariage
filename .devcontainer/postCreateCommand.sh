#!/bin/bash
echo "## Install Azure Static Web Apps CLI"
npm install -g @azure/static-web-apps-cli@latest 

echo "## Install packages and generate env file if it doesn't exist"
pushd www 
npm install 
cp -u .env.file .env
popd

echo "## Generate local settings"
pushd backend/functions 
echo '{\"IsEncrypted\":false,\"Values\":{\"FUNCTIONS_WORKER_RUNTIME\":\"dotnet-isolated\"}}' > local.settings.json
popd


echo "## Install Dart Sass"
pushd /tmp
curl -O --location https://github.com/sass/dart-sass/releases/download/1.79.4/dart-sass-1.79.4-linux-x64.tar.gz
tar xvf dart-sass-1.79.4-linux-x64.tar.gz 
sudo cp -r dart-sass/* /usr/local/bin/
popd