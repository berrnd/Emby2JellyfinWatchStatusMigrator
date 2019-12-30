dotnet publish -r win-x64 -c release
dotnet publish -r linux-x64 -c release /p:PublishReadyToRun=false
dotnet publish -r linux-arm -c release /p:PublishReadyToRun=false
dotnet publish -r osx-x64 -c release /p:PublishReadyToRun=false

Remove-Item -Recurse -Force ".release"
md ".release"

$version = (Get-Item "bin\Release\netcoreapp3.1\win-x64\publish\Emby2JellyfinWatchStatusMigrator.exe").VersionInfo.FileVersion

Compress-Archive -Path "bin\Release\netcoreapp3.1\win-x64\publish\Emby2JellyfinWatchStatusMigrator.exe" -DestinationPath ".release\Emby2JellyfinWatchStatusMigrator_$($version)_win-x64.zip"
Compress-Archive -Path "bin\Release\netcoreapp3.1\linux-x64\publish\Emby2JellyfinWatchStatusMigrator" -DestinationPath ".release\Emby2JellyfinWatchStatusMigrator_$($version)_linux-x64.zip"
Compress-Archive -Path "bin\Release\netcoreapp3.1\linux-arm\publish\Emby2JellyfinWatchStatusMigrator" -DestinationPath ".release\Emby2JellyfinWatchStatusMigrator_$($version)_linux-arm.zip"
Compress-Archive -Path "bin\Release\netcoreapp3.1\osx-x64\publish\Emby2JellyfinWatchStatusMigrator" -DestinationPath ".release\Emby2JellyfinWatchStatusMigrator_$($version)_osx-x64.zip"
  
pause
