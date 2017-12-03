"E:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -nographics -nolog -buildWindowsPlayer Builds\clients\windows\client.exe
"E:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -nographics -nolog -buildOSXUniversalPlayer Builds\clients\osx\client
"E:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -nographics -nolog -executeMethod BuildServer.PerformBuild
"E:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -nographics -nolog -buildLinuxUniversalPlayer Builds\clients\linux\client