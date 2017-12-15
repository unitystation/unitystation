## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Attempting build of UnityStation for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildLinuxUniversalPlayer  "$(pwd)/Build/linux/UnityStation.bin" \
	-quit

ls -l $(pwd)/Build/linux/
rc0=$?
echo "Build logs (Linux)"
cat $(pwd)/unity.log

exit $(($rc0))