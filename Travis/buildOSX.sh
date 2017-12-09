echo "Attempting build of ${UNITYCI_PROJECT_NAME} for OSX"
/Users/travis/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildOSXUniversalPlayer "$(pwd)/Build/osx/${UNITYCI_PROJECT_NAME}.app" \
	-quit

rc0=$?
echo "Build logs (OSX)"
cat $(pwd)/unity.log

exit $(($rc0))