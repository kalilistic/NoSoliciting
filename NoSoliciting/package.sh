#!/bin/sh

# remove old pack dir
rm -rf pack

# make temp dir and go to it
mkdir -p pack/temp
cd pack/temp || exit

# copy the dlls, defs, and the manifest
cp ../../bin/Release/NoSoliciting.dll ../../bin/Release/YamlDotNet.dll ./
cp ../../definitions.yaml default_definitions.yaml
cp ../../NoSoliciting.json ./

# make sure none of them are marked executable
chmod -x ./*

# zip them
zip ../latest.zip ./*

# move the manifest next to the zip
mv NoSoliciting.json ../

# remove everything
rm ./*

# go back up and remove the temp dir
cd ..
rmdir temp
