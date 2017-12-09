## Make the builds
# Recall from install.sh that a separate module was needed for Windows build support
echo "Installing UnitySetup-Windows-Support-for-Editor-$VERSION"
sudo installer -dumplog -package "UnitySetup-Windows-Support-for-Editor-$VERSION.pkg" -target /
echo "Attempting build of ${UNITYCI_PROJECT_NAME} for Windows"
/Users/travis/Applications/Unity/Unity.app/Contents/MacOS/Unity \
	-batchmode \
	-nographics \
	-silent-crashes \
	-logFile $(pwd)/unity.log \
	-projectPath "$(pwd)/${UNITYCI_PROJECT_NAME}" \
	-buildWindowsPlayer "$(pwd)/Build/windows/${UNITYCI_PROJECT_NAME}.exe" \
	-quit

rc0=$?
echo "Build logs (Windows)"
cat $(pwd)/unity.log

exit $(($rc0))