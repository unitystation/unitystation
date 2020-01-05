# Creating and Using Tile Pallettes

This guide will focus on creating and adding tile assets to tile palettes and using the LevelBrush to modify/create a tilemap.

## Creating a Tile Asset with Prefab
To create a new tile asset that can be used within a tile palette we must first copy/create a tile asset. It is okay to copy a pre-existing one as all tile assets are tied to the same script. For this guide we will create a duplicate of the `AirLock` tile asset.
* To start, copy one of the tile assets in the `Unity station\unitystation-develop\UnityProject\Assets\Tilemaps\Tiles\Doors` folder:

![](https://i.imgur.com/LNkY0ca.gif)

* Next we go into Unity and select the proper prefab on the new tile asset:

![](https://i.imgur.com/icmjVer.gif)

The tile asset has now been fully created and is usable with tile palettes. As long as this method is used a preview sprite is automatically created.

## Adding Tile Assets to Tile Palette
In order to use the LevelBrush with new tiles it is essential to add the tile to the appropriate tile palette.
* Go to `Window > Tile Palette`:

![](https://i.imgur.com/Xbrf7dU.png)

You should see a window similar to this one:

![](https://i.imgur.com/eKj7nks.png)

* Click `Canisters and Tanks` and select the appropriate tile palette. For the next step we will use `Doors`:

![](https://i.imgur.com/x0Kw4ZA.png)

* For this step the `AirLock` tile has been removed. We can add it back by selecting the arrow in the toolbar, then clicking `Edit`, then selecting the empty tile space and finally selecting the `AirLock` tile in the Inspector:

![](https://i.imgur.com/DCOBf4f.gif)

Remember to deselect `Edit` once the tile has been added to ensure the palette is finalized.

#### Note: If you get this error: `Unsupported texture format - needs to be ARGB32, RGBA32, RGB24, Alpha8 or one of float formats` then follow these steps:

 - Delete the asset
 - Find the sprite sheets that your prefab uses in the Project Files and select them to access the Import Settings
 - Make sure Read / Write is enabled and scroll to the bottom and turn off compression (usually it is set to normal so set it to none)
 - Apply and then retry the process

## Painting Using a Tile Palette
Painting tiles using a palette is relatively easy due to the LevelBrush.
* Go to `Window > Tile Palette`:

![](https://i.imgur.com/Xbrf7dU.png)

You should see a window similar to this one:

![](https://i.imgur.com/eKj7nks.png)

* Click `Canisters and Tanks` and select the appropriate tile palette. For the next step we will use `Floors`:

![](https://i.imgur.com/efnvpTN.png)

* Now all you have to do is select the paintbrush in the toolbar as well as the tile you wish to paint with and draw on the tilemap:

![](https://i.imgur.com/qy4tf9r.gif)