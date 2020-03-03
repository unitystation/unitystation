# Directory Structure

All main project files are contained in the /Assets folder.
The structure can be viewed below:

## Current structure
### General
|  Folder 		|  Description 	|
|---			|---	|
|../Tools/  | All non-unity tools, such as coding style presets|
|../Docs/  | All Github-rendered documentation except for the licence|
|../Travis/  | All config files for our CI Travis |

### Unity
All Unity related files are contained in the /UnityProject folder.

|  Folder 		|  Description 	|
|---			|---	|
|/Assets/Animations	| Contains most of the animations used in-game 	|   	
|/Assets/Data   	|   	|
|/Assets/Interface   	|   	|
|/Assets/Light2D   	| Contains the Light2D plugin  	|
|/Assets/Materials   	|   	|
|/Assets/_Plugin-Name_  | Contains all assets and scripts from a certain plugin |
|/Assets/Prefabs   	| Contains prefabs for most things except for Objects  	|
|/Assets/Resources  	| Contains a lot of files that need to be loaded during the game	|
|/Assets/scenes  	| Contains the different scenes (every scene is a separate folder at the moment)  	|
|/Assets/Scripts   	| Contains most of the scripts used in-game 	|
|/Assets/Scripts/Editor   | Contains most of the scripts used in the editor 	|
|/Assets/Scripts/Tilemaps | Contains all Scripts for TMS |
|/Assets/shaders   	|   	|
|/Assets/Sounds  	| Contains most of the in-game sounds 	|
|/Assets/Textures   	| Contains a lot of sprites  	|
|/Assets/UI     	| Contains UI related content  	|
|/Assets/Tilemaps  	| Contains most things directly related to the TileMapSystem (TMS)  	|
|*/Resources/           | This is a Resources folder, all content is acted upon as if it was in /Assets/Resources

### Todo
The following things should be changed to get a more clear directory structure.

**plugins**

|  Folder 		|  Description 	|
|---			|---	|
|/Assets/_Plugin-Name_  | Contains all assets <del>and scripts</del> from a certain plugin |
|/Assets/Scripts/_Plugin-Name_ | Contains all Scripts from a certain plugin |
|/Assets/Scripts/_Plugin-Name_/Editor | Contains all editor Scripts from a certain plugin |

**Resources**<br>
We should create a script to recursively load all resources from /Assets/Resources/_Sub-Folder_
for ex. RecursiveResourceLoad(Subfolder, Filename)

This would enable us to put all resources in a subfolder under /Assets/Resources <br>
`One Folder To Find Them, And In-Game Load them`

Eventually, even plugin related resources could be moved here, but that may require editing the scripts first, which may or may not be a good thing.

**Prefabs**<br>
Almost all prefabs are networked and should/will need to be in a resource folder eventually. When a good standardised script is created to recursively load resources from a /Resources/Sub-Directory, we can move most, if not all, prefabs over to the resources directory