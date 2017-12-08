
## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Installing "UnitySetup-Linux-Support-for-Editor-$VERSION.pkg"
sudo installer -dumplog -package "UnitySetup-Linux-Support-for-Editor-$VERSION.pkg" -target /

echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildWindowsPlayer "$(pwd)/Build/linux/${UNITYCI_PROJECT_NAME}.exe" \
	-quit

rc0=$?
echo "Build logs (Linux)"
cat $(pwd)/unity.log

exit $(($rc0))