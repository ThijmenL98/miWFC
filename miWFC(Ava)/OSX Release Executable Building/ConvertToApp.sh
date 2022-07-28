#!/bin/bash
mkdir "$(dirname "$PWD")/bin/Release/net6.0/out/"

APP_NAME="$(dirname "$PWD")/bin/Release/net6.0/out/miWFC.app"
PUBLISH_OUTPUT_DIRECTORY="$(dirname "$PWD")/bin/Release/net6.0/osx-x64/publish/."
INFO_PLIST="Info.plist"
ICON_FILE="icon.icns"

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME"

mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
cp "$ICON_FILE" "$APP_NAME/Contents/Resources/icon.icns"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"

echo "Done"
read