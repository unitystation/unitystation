# Building FAQ

This section is to answer questions regarding problems or questions building the project. 

## Type or namespace name "Tomyln" could not be found

If you found this issue when trying to open the project for the first time, you can solve it by opening Unity in safe mode and clicking on Nuget > Restore Nuget packages from the top bar.
Relevant github issue: https://github.com/unitystation/unitystation/issues/10147

## Forever loading bar when pressing play in the editor

Due to how we turn on Start asset editing when in play mode ( To prevent mid game compile ), This results in a loading bar,
Do not worry though because you can move the loading bar of the way it doesn't block your input into the Unity editor

If you would like to edit assets while the Editor is in play mode simply go to 
Tools > StopAssetEditing