# How to make paletteable sprites

This guide will help you make sprites that have their colors defined by a palette that can be altered at runtime. 
Even if you are starting with an ordinary sprite sheet, the process is more of an art than a science. 
That being said, with some practice the results are very neat:

![Early example of paletteable sprites](https://i.imgur.com/c4FGRPC.gif)

With this system it could be possible to code dyes, paints, spells, or other features that change the color of objects in the middle of the game!

Note: At time of writing, this system is only confirmed to work with items which make use of the `ItemAttributesV2` script.

## Creating the paletteable sprite texture

The colors of a paletteable sprite indicate which swatches of the palette to blend together and how to blend them. 
So, in their unshaded form, they look almost nothing like what the results might produce. 
Understandably, it would be difficult to create something like this using an ordinary image editor. 
With that in mind, a custom image editing tool has been made to work with this format. 

It is a web tool available in ``Tools/paletteable sprite editor/``:

![Screenshot of the paletteable sprite editor](https://i.imgur.com/ywW5g8e.png)

### Features of the tool
On the top is where a paletteable sprite can be loaded in for previewing or editing. In fact, any image can be loaded here 
and that is very useful for converting an ordinary sprite into a paletteable one.

In the upper-left is the palette manager for the tool. 

* Select the palette colors by clicking on the colored boxes.

* Determine which palette swatches the cursor will draw from by selecting swatches for A and B.

* Determine how the A and B swatches are blended for drawing with the cursor by moving the slider left and right

* The color next to the slider provides a preview of the color that the cursor will draw

* Add and remove swatches 


In the lower-left are some actual-size preview panes. 

* The top image under **Preview** shows what the paletteable sprite currently in the canvas will produce with the colors currently selected in the palette.

* The lower image under **Paletted** shows what the paletteable sprite looks like in its raw form. This is used to export the sprite from the tool by right-clicking and saving the image to disk.

### Typical workflow of using the tool

Suppose we want to make this ordinary sprite sheet into being paletteable:

![ordinary sprite sheet](https://i.imgur.com/6ckpKah.png)

1. Optional: Load a sprite into the editor by browsing for the sprite and clicking `Load Raw` at the top. 

    * This can be an ordinary or a palettable sprite, although an ordinary sprite will not look as expected in the canvas of the tool. Loading an ordinary sprite can still be useful because you can trace the shape of the sprite, so if you choose that. You will want to open the original in another window.

    * If loading in a paletteable sprite for editing, **be sure to add or remove swatches so that the palette size is correct *before* loading the sprite!**

![ordinary sprite sheet loaded into the tool](https://i.imgur.com/xHvkmC7.png)

Note: In the case of large sprites, you may need to slice them up to get them to fit into the canvas of the tool.

2. Add, remove and edit the colors in the palette to be ones that you think describe the object. 
For the best effect, colors that should always a blend of two other colors should not be their own palette color, because the blending should take care of that. 
For instance, in a grey jumpsuit, you might have a medium grey for the main color of the clothing, black for the shadows, black for the dark part of the belt, and white for the belt buckle.

This step is very much up to artistic interpretation.

![selected palettes for grey jumpsuit](https://i.imgur.com/QKPFGT8.png)

3. Use the slider to mix the swatches to make the colors you'd like to paint. Left-click in the canvas to draw a pixel. Right-click in the canvas to remove a pixel. If a pixel refuses to paint when left-clicked try right-clicking first to remove it (known bug).

For example, suppose swatch 0 is white and swatch 1 is black. 
If you want to paint grey because it is a blend of those colors, you can set A to swatch 0 and B to swatch 1 and blend as appropriate. 
If you want to paint white, you can set A or B (or both) to swatch 0 and set the blend level appropriately.

![example of paletted jumpsuit in tool](https://i.imgur.com/W1z5ob8.png)

4. When you want to check that your work is going well, it is recommended to change the palette colors to different values to 
verify that the blending works as expected. 
You might realize that it would make sense to combine two colors into one swatch or to make one swatch into two to allow for more control.

![example of trying different palette colors](https://i.imgur.com/LS7pVPr.png)

5. Once you are done editing the sprite, you can export it by right-clicking into the **Paletted** image pane in the lower-left and
saving it to disk. 

Note: The editor does not support alpha editing, every pixel is either 0% or 100% opaque. 
This is a quirk of the editor, the paletting shader fully supports alpha in the sprites. 
Consider using an external image editor to apply alpha as needed. (Or, making a better editor 😉)

## I have a paletteable sprite texture. Now what?

It takes a few steps to import it, set the palette colors, and tell the various bits to use the palette.

1. First, load the texture into unity and slice it into sprites as you would any sprite.
2. Apply the new sprite to the SpriteDataSO(s) for the item you would like.
3. Enable the checkbox(es) for `Is Palette` in the SpriteDataSO(s).
4. If the item is clothing, find the item's Clothing Data and set the palette size and colors there and enable the checkbox for `Is Paletted`.
5. Go into the prefab of the item with `Item Attributes V2` And set the palette size and colors there, and also check `Is Paletted`. 
   * In the `Sprite` child(ren) of the object, set the size and colors of `Palette`.

### Dev notes 

* The general idea is that each instance of an item can have a distict selection of colors for its palette. This is the rationale behind having a palette on `Item Attributes V2`. However, the sprite handler currently manages its own palette drawn in most cases from sprite SO. A good improvement would be to draw from the sprite SO or clothingData (or other sources of a "default" palette, and then allow the `ItemAttributes V2` palette to override it. 
* Various rendering components need access to the palette to apply it to the shader they control. Search for `_ColorPalette` to see where this occurs.
* It would be convenient for changes to propagate from the `Item Attributes V2` of the instance to the spriteHandlers associated with the item. Currently, there is a right-click menu for `Item Attributes V2` which once used to propagate them starting in `PropagatePaletteChanges` but it has since broken (and even then, it only worked for Clothes). Making that work completely again may be a welcome fix.

## How the shader works

For each pixel:

* R component is used to select the index of swatch A. 0-31 => swatch 0, 32-63 => swatch 1, etc.
* G component is used to select the index of swatch B.
* B component is used to lerp from the color of A to the color of B in RGB color space. 0 => 100% A, 128 => 50% - 50%, 255 => 100% B.
* A component is used directly for the alpha of the output.

The shader currently only works with a palette length of exactly 8 but it could in theory work with 256 colors. There are a few magic number 8s that would need to be changed as well to increase the palette size and/or make it dynamic.
