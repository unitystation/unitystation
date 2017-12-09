## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildWindows64Player "$(pwd)/Build/windows/${UNITYCI_PROJECT_NAME}.exe" \
	-quit

ls -l $(pwd)/Build/windows/
rc2=$?
echo "Build logs (Windows)"
cat $(pwd)/unity.log

## Make the builds
# Recall from install.sh that a separate module was needed for Linux build support
echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildLinuxUniversalPlayer  "$(pwd)/Build/linux/${UNITYCI_PROJECT_NAME}.bin" \
	-quit

ls -l $(pwd)/Build/linux/

rc3=$?
echo "Build logs (Linux)"
cat $(pwd)/unity.log

exit $(($rc2|$rc3))