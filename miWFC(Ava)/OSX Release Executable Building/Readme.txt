Use the following command to publish osx files in the outer directory:
  dotnet publish -r osx-x64 -c Release /p:PublishSingleFile=true -p:UseAppHost=true

Then use ConvertToApp.sh

The .app file is now located in /bin/Release/net6.0/out/miWFC.app

Then open the created .app on a MacOS device and use:
  chmod +x miWFC.app/Contents/MacOS/miWFC