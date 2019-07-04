## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Attempting build of UnityStation for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildWindows64Player "$(pwd)/Build/windows/UnityStation.exe" \
	-quit

ls -l $(pwd)/Build/windows/
rc0=$?
echo "Build logs (Windows)"
cat $(pwd)/unity.log

exit $(($rc0))