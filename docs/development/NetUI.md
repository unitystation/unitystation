This subsection will be describing NetUI elements and how to use them.

## Important stuff
1. `NetTab` stores all `NetUIElement` at the start in the dictionary; because of that, we can use `netTab["elementName"]` to get reference to elements. However, it gives us a restriction - all `NetUIElement` must have unique names in current tab. If you have two `NetLabel` named "text" as children of `NetTab` gameobject - you will get duplicate dictionary key error.
2. `NetUIElement` has two methods for setting value - `Value` and `SetValue`. The only difference is that `SetValue` calls net update immediately.
<br>
If you don't understand how to use a particular component - check examples to see how they are used in existing UI.
If you still can't understand something - be sure to ask on Discord.

## NetworkTabTrigger
`NetworkTabTrigger` is a class that allows you to open UI window on player Interaction. The class is abstract, so you will have to inherit it.
### Usage:
1. Put an inherited `NetworkTabTrigger` component on item in scene that will open your UI.
2. Set NetTabType in inspector so it opens correct window on interaction.
### Examples:
Just check classes that inherit `NetworkTabTrigger` - `APCInteract`, `CargoConsole`, `NukeInteract`, etc.

## NetLabel
NetLabel is used to sync Text component between clients.
### Usage:
1. Add `NetLabel` to gameobject with `Text` component.
2. When you need to change the text, server should run `netLabel.SetValue = "message";`. This will update text component on all clients.
### Examples:
NetLabel is very simple and straight forward, but if you need to see examples of usage - check out `GUI_APC` or `GUI_Spawner`.

## NetColorChanger
NetColorChanger syncs color of any `Graphic` element (i.e `Image`). Takes color hex value as a string.
### Usage:
1. Add `NetColorChanger` to gameobject with `Graphic` component.
2. When you need to change color, server should run `netColorChanger.SetValue = "ffffff";` where `ffffff` is new hex color.
### Examples:
Current `GUI_APC` makes heavy usage of that component, `GUI_CargoPageStatus` contains NetColorChanger as well.

## NetButton
NetButton is a component, that lets client call server execution on some code when pressing UI button.
### Usage:
1. Add `NetButton` to gameobject with `Button` component.
2. Change button's `OnClick()` to `NetButton.ExecuteClient`.
3. Change netButton's `ServerMethod()` to method you wish to call as a server.
In the end, it should look like that: <br>
![](https://cdn.discordapp.com/attachments/295186861377323009/583209885299245056/unknown.png) <br>
### Examples:
`TabCargo` prefab has lots of buttons with `NetButton` component.

## NetPages
NetPages is a concept of making UI in several pages instead of one ([cargo console](https://www.youtube.com/watch?v=fFuLGzgH9Ck)). It is done by switching on/off gameobjects that represent sub-page in one window. The structure looks like this:<br>
![](https://cdn.discordapp.com/attachments/295186861377323009/583235140038426627/unknown.png)<br>
Where current tab's gameobject will be enabled and all others disabled.
### Usage:
1. Structure your gameobjects like on screenshot above. NetTab is your main window with `NetTab` component.
2. Put `NetPageSwitcher` on NetPages gameobject. Here you can set pages and default one, if you dont set them - it will be done at the `Init()`
3. Put `NetPage` component on all of your subpages. You can inherit this class if you wish to make some page-specific code.
To switch between pages, you will need to call `SetActivePage(NetPage)`, `NextPage()` or `PreviousPage()` methods of your `NetPageSwitcher`
### Examples:
`GUI_Spawner` and `GUI_Cargo` both use NetPages.

## Dynamic Lists
Dynamics lists are net synced lists of gameobjects.
For now, there are two types of dynamic lists - `ItemList` which stores prefabs (not actual instances) and `SpawnedObjectList`that contains objects that are spawned ingame.
### Usage:
1. Add `SpawnedObjectList` to gameobject that will act as a holder to list elements.
2. Set its EntryPrefab in the editor. If you won't set it, it will try to Resource.Load gameobject with name `%NetTabType%Entry` (i.e. CargoEntry).
3. Use `AddObjects(List<GameObject>)` to populate the list with items you need. <br>
If you need to access entry, you can do `TypeYouNeed entry = myDynamicList.Entries[i] as TypeYouNeed`
### Examples:
`GUI_Spawner` which uses `ItemList`. <br>
Cargo uses inherited class - `GUI_CargoItemList`. You can see usages of it in `GUI_CargoPageSupplies` and `GUI_CargoPageCart`.
