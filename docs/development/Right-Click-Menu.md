# Right Click Menu

This page describes how the new right click menu works. The new system uses Scriptable Objects and allows for dynamic changes to an object's right click menu (allows changing what options are presented based on an object's state). It also allows overriding the text, sprite, and background color of the menu items and is designed to be developer friendly and easy to quickly set up.

If you have any issues or akwardness trying to use it, or you want to suggest an improvement, feel free to reach out to @chairbender on Discord or create an Issue.

## Overview
All possible right click options are defined as ScriptableObjects. These define the appearance of the right click option but do not define what happens when the option is selected. These are all in Resources/ScriptableObjects/Interaction/RightclickOptions.

To ensure consistent ordering, there is also a RightClickOptionOrder ScriptableObject which allows defining the display order of RightClickOptions. You can modify this in the Right click canvas.prefab's Rightclick Manager component.

RightclickManager (formerly Rightclick) lives on Right click canvas.prefab and manages all right click logic, similarly to what Rightclick was doing before.

By default, an object's top-level right click menu button will use the first sprite on the object and a default color and name. You can override these by attaching a RightClickAppearance component. You only need to put this on objects whose right click icon appearance you want to change from the default behavior.

There are 2 ways for a component to define right click menu options on its object.

## Generate Options with IRightClickable
A component can implement IRightClickable to define which options should be shown based on its current state. All that
is needed is to implement the method.

To use this in a component:
1. Create a RightClickOption for your option if it doesn't already exist, and set up its appearance. You can do this in Unity by the Create > Interaction > Right click option menu item. You must do this in Resources/ScriptableObjects/Interaction/RightclickOptions.
1. Implement IRightClickable's GenerateRightClickOptions method. You must create a RightClickableResult and use the available methods to add right click options based on the object's state. You can return null
if no options should be generated. It is recommended to use the method-chaining features of RightClickableResult to
improve conciseness and readability, if possible.

Using the methods on RightClickableResult, it is also possible to override the default appearance of the RightClickOption, such as modifying its background color or icon.

You can have this on multiple components on a given object - all of their right click options will be added together.

Here's a simple example for a behavior whose right click option doesn't depend on its state:
```csharp
public class ItemAttributes : NetworkBehaviour, IRightClickable
{
  //implementation of the interface method. This is invoked by RightClickMenu when
  //it's time to display menu options.
  public RightClickableResult GenerateRightClickOptions()
  {
    //Use method chaining to simplify readability.
    //Note that the "Examine" string is the name of the RightClickOption ScriptableObject
    //we want to display.
    return RightClickableResult.Create()
      .AddElement("Examine", OnExamine);
  }

  void OnExamine()
  {
    //...performs the examination logic
  }
}
```


Here's a more complex example in PickUpTrigger where the options shown depends on the state:
```csharp
public class PickUpTrigger : InputTrigger, IRightClickable
{
  //implementation of the interface method. This is invoked by RightClickMenu when
  //it's time to display menu options.
  public RightClickableResult GenerateRightClickOptions()
  {
    if (PlayerManager.LocalPlayerScript.canNotInteract())
    {
      //return null if no options should be shown
      return null;
    }
    var player = PlayerManager.LocalPlayerScript;
    UISlotObject uiSlotObject = new UISlotObject(UIManager.Hands.CurrentSlot.inventorySlot.UUID, gameObject);
    if (UIManager.CanPutItemToSlot(uiSlotObject))
    {
      if (player.IsInReach(this.gameObject, false))
      {
        //using method chaining to enhance readability
        return RightClickableResult.Create()
            //we specify the name of the RightClickOption ScriptableObject
            .AddElement("PickUp", GUIInteract);
      }
    }

    return null;
  }

  void GUIInteract()
  {
    //...performs the pickup logic
  }
}
```

Or, if you don't want to use method chaining, that can be done as well:
Here's a more complex example in PickUpTrigger where the options shown depends on the state:
```csharp
public class PickUpTrigger : InputTrigger, IRightClickable
{
  //implementation of the interface method. This is invoked by RightClickMenu when
  //it's time to display menu options.
  public RightClickableResult GenerateRightClickOptions()
  {
    var result = RightClickableResult.Create();
    if (PlayerManager.LocalPlayerScript.canNotInteract())
    {
      //return null or empty result if no options should be shown
      return result;
    }
    var player = PlayerManager.LocalPlayerScript;
    UISlotObject uiSlotObject = new UISlotObject(UIManager.Hands.CurrentSlot.inventorySlot.UUID, gameObject);
    if (UIManager.CanPutItemToSlot(uiSlotObject))
    {
      if (player.IsInReach(this.gameObject, false))
      {
        //add to the result, not using method chaining.
        result.AddElement("PickUp", GUIInteract);            
      }
    }

    return result;
  }

  void GUIInteract()
  {
    //...performs the pickup logic
  }
}
```


## Use the [RightClickMenu] Attribute
This should only be used for development, as it can create performance issues if there are lots
of usages of it. You can put this attribute on any no-arg method of a component and a right click
menu option will be generated which invokes that method. This can be useful if you have a method
you want to be able to invoke in game at will.

For example, if we have a lot of useful development / debugging methods we'd want to invoke on an object,
we could create a developer-only component and attach it to our prefabs (remove it for release builds) to
provide a sort of "dev menu".

To use this approach:
1. Simply put the attribute on the no-arg method of the component you want to invoke. Refer to the
various parameters of RightClickMenu to see how you can customize the color, label, or icon of the
displayed option. By default, the attribute will show a question mark and be named after the method
it is attached to.

For example, here is how it is used in BatterySupplyingModule to allow toggling of charge:
```csharp
public class BatterySupplyingModule : ModuleSupplyingDevice
{
  
  //We will see an option called "ToggleCharge" that invokes this method.
  [RightClickMethod]
  public void ToggleCharge()
  {
    ToggleCanCharge = !ToggleCanCharge;
  }
}
```