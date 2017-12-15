echo "Attempting build of UnityStation for OSX"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildOSXUniversalPlayer "$(pwd)/Build/osx/UnityStation.app" \
	-quit

ls -l $(pwd)/Build/osx/
rc0=$?
echo "Build logs (OSX)"
cat $(pwd)/unity.log

exit $(($rc0))