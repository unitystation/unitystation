## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Windows"
/Users/travis/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildWindowsPlayer "$(pwd)/Build/windows/${UNITYCI_PROJECT_NAME}.exe" \
	-quit

rc2=$?
echo "Build logs (Windows)"
cat $(pwd)/unity.log

## Make the builds
# Recall from install.sh that a separate module was needed for Linux build support
echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Linux"
/Users/travis/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildLinuxPlayer "$(pwd)/Build/linux/${UNITYCI_PROJECT_NAME}.exe" \
	-quit

rc3=$?
echo "Build logs (Linux)"
cat $(pwd)/unity.log

exit $(($rc2|$rc3))