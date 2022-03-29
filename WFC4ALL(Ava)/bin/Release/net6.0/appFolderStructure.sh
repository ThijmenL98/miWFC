#!/bin/bash
APP_NAME="C:/Users/thijm/Documents/Git Repositories/thesis/WFC4ALL(Ava)/bin/Release/net6.0/out/WFC4All.app"
PUBLISH_OUTPUT_DIRECTORY="C:/Users/thijm/Documents/Git Repositories/thesis/WFC4ALL(Ava)/bin/Release/net6.0/osx-x64/publish/."
INFO_PLIST="C:/Users/thijm/Documents/Git Repositories/thesis/WFC4ALL(Ava)/bin/Release/net6.0/Info.plist"
ICON_FILE="C:/Users/thijm/Documents/Git Repositories/thesis/WFC4ALL(Ava)/bin/Release/net6.0/icon.icns"

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME"

mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "$ICON_FILE" "$APP_NAME/Contents/Resources/icon.icns"
cp "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"

$SHELL