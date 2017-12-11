#! /bin/sh
BASE_URL=http://download.unity3d.com/download_unity
HASH=46dda1414e51
VERSION=2017.2.0f3

Echo $BASE_URL/$HASH/MacEditorInstaller/Unity-$VERSION.pkg $BASE_URL/$HASH/MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg $BASE_URL/$HASH/MacEditorTargetInstaller/UnitySetup-Linux-Support-for-Editor-$VERSION.pkg | xargs -n 1 -P 8 wget -q

echo "Installing Unity"
sudo installer -dumplog -package "Unity-$VERSION.pkg" -target /