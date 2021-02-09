### Development Protips

Various tips to help speed up development and share random knowledge that doesn't deserve its own page. Feel free to edit!

#### General

1. Familiarize yourself with our [Development Gotchas](Development-Gotchas-and-Common-Mistakes.md)

#### Faster Building and Testing

1. Only include the smallest station in the build / rotation. Including all maps will needlessly increase build / start times. 
    * In `Lobby` scene,
edit `NetworkManager`, change `Online Scene` to `PogStation` (smallest map at the moment). 
    * Edit `Assets/StreamingAssets/maps.json`, removing 
all but PogStation from `lowPopMaps`
    * When you build, uncheck all but StartUp, Lobby, and PogStation
    * Don't commit these changes.
