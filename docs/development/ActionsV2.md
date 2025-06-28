!!! warning
    V1 Actions are considered obsolete by now. If you see an issue with the old actions system, DO NOT TOUCH IT!!! Port the actions to the new V2 system.


# UI Actions

In SS13, players can preform specific actions by pressing a button on their hud that is usually placed at the top left of their screen. These buttons are named "Action Buttons" or "UI Action" inside the codebase.

Every mob and mind has their respective action tracker (`ActionManager`), where they track the assigned behaviors and data related to them. The action tracker on the mind carries actions over across bodies, and can even be used for adding gameplay elements to ghosts; while the action tracker on the body only has local actions that are available to that body only.

Every action expects a mouse position (`Vector2`), even if the user is not doing anything that requires the active targeting.

# Assigning Actions - The Basics.

Like we've discussed, there's two places actions are being tracked on.

```cs

ActionManager mindTracker = PlayerManager.LocalMindScript.PlayerButtonedActions; // will appear with a blue background
ActionManager bodyTracker = PlayerManager.LocalPlayerScript.PlayerButtonedActions;

```

To register an action to one of these trackers, we must first create an `ActionButtonData`. We can create those programmatically, but we can also create those in the inspector as we'll see later on.

`ActionTracker` provides us a method to handle the creation `ActionButtonData`

```cs
private async UniTask SetupDebugActions(SpriteDataSO ghostDisappearSpriteSO, SpriteDataSO ghostAppearSpriteSO)
		{
			var playerScript = GetComponent<PlayerScript>(); // we want the tracker that's on the body.

			playerScript.PlayerButtonedActions.RegisterNewAction("player_become_invisible", "Become Invisible",
				"Make yourself invisible to other players.", ActionTriggerType.ServerOnly, ghostDisappearSpriteSO, v =>
				{
					Alpha = 0.1f;
					Chat.AddExamineMsg(gameObject, "You have become invisible to other players.");
				}, cooldownTime: 8f);

			playerScript.PlayerButtonedMindActions?.RegisterNewAction("player_become_visible", "Become visible",
				"Make yourself visible to other players.", ActionTriggerType.ServerOnly, ghostAppearSpriteSO, v =>
				{
					Alpha = 1f;
					Chat.AddExamineMsg(gameObject, "You have become visible to other players.");
				}, cooldownTime: 8f);
		}
```

In this example here, we are registering two new actions that allow the user to become invisible and visible on a script like `BodySpritesInvisbility`.

We first assign a unique ID (`"player_become_invisible"`) that is going to be used for telling the server/client which action we want to run, then the action's display name and description.

`ActionTriggerType` lets us determine if we want to trigger actions on the server, client, or both. We recommend running *all* actions on the server, but if you have a function that needs to do something local like messing with the UI, spicing visuals, etc, you can always change it to `Both` or `ClientOnly` if needed.

We can also set the sprite of the button with `SpriteDataSO`. If this is set as null, an ERROR placeholder sprite will be automatically displayed instead.

After setting up all the data, it's time to set the real meat and potatos of this whole function, the `Action<Vector2>`. These can be assigned as lambdas (like in the example), or can you can assign a function directly, as long as it's only arguments include a `Vector2`.

```cs

//lambda
playerScript.PlayerButtonedActions.RegisterNewAction("player_show_example_1", "Example 1",
				"Make yourself invisible to other players.", ActionTriggerType.Both, null, v =>
				{
					Chat.AddExamineMsg(gameObject, "This is a lambda example!");
				}, cooldownTime: 8f);

//passing a function
playerScript.PlayerButtonedActions.RegisterNewAction("player_show_example_1", "Example 1",
				"Make yourself invisible to other players.", ActionTriggerType.Both, null, myFunc, cooldownTime: 8f);

void myFunc(Vector2 mousePosition)
{
    Chat.AddExamineMsg(gameObject, "hello from my function!");
}
```

Lastly, we can set a cooldown time if we want. By default it's 0, and anything under 0.085 seconds wont register.

This is a very basic approach to registering new commands to a player on their body.

if we want to unregister a command, we can simply just use `UnregisterAction` on the tracker.

```cs

//unregistering example
public void UnregisterAllActions()
		{
			foreach (ActionButtonData actionData in ListOfActions)
			{
				TargetActionManager.UnregisterAction(actionData);
			}
		}
```

# Assigning Actions - The Automated Way

Let's say you have a flashlight, where you want it to have the ability to blind other players from a distance. How would you hook up that logic for it? How would you manage the scenario of the player not longer holding the flashlight?

The easiest way to handle this without rewriting code is to use trackers that implement the `IActionButtonTracker` interface. The most common one you'll use will be `ItemSlotActionTracker`.

![](../assets/images/ActionsV2/component_showcase_tracker.png)

You just create a new entry, fill in the data that you need for this action on the left, then assign the function that will be run on the right. Simple, Right?

This tracker will automatically cover all cases needed for registering and unregistering actions from bodies, so you don't have to worry about!

!!! warning
    You must pass in a function that only has a Vector2 as it's parameter, anything else wont work.


# Boring Explanation - Action Manager

The `ActionManager` is a network-enabled component that handles registration, management, and execution of actions in a client-server architecture. It's built using Mirror networking solutions for Unity.

Whenever an action gets registered or unregistered, `ActionButtonManager` is informed about these changes, and updates the UI accordingly.

## Key Components

### Action Registration Types

Actions can be registered in three ways:
- `ServerOnly` - Executes only on the server
- `ClientOnly` - Executes only on the client
- `Both` - Executes on both client and server

### Action Button Data

Each action contains:
- Unique ID
- Display name
- Description
- Trigger type
- Cooldown time
- Animated icon catalogue
- Ghost usage permission flag

### Cooldown System

The cooldown system in the ActionManager ensures that actions cannot be executed repeatedly within a set amount of time. It is implemented using a SyncList of CooldownInfo objects, which are synchronized across the network.

Key Points:

- Minimum cooldown time for a cooldown to be registered: 0.085 seconds
- Cooldowns are synchronized across network
- Tracks cooldown state and remaining time
- Auto-cleans expired cooldowns

## Usage Example

```csharp
// Register a new action
actionManager.RegisterNewAction(
    newID: "jump",
    displayName: "Jump",
    desc: "Makes the character jump",
    triggerType: ActionTriggerType.ServerOnly,
    Icon: jumpIcons,
    logic: (Vector2 pos) => { /* Jump logic */ },
    canBeUsedWhileGhosting: false,
    cooldownTime: 1.0f
);

// Unregister an action
actionManager.UnregisterAction(actionData);
```

## Authority

Actions are synced owners of the mind and player body only.

Because `CmdTriggerAction` uses Mirror RPCs to function, other users cannot run functions on managers they do not have authority or observer powers over.

## Annoying Details

Clients can lose their client sided actions' logic upon relogging into the game. Programmers who decide to implement client sided actions must reassign that logic themselves to clients. You don't have to worry about this on the server side, as the logic will always be tracked on its side (unless the server goes up in flames and restarts, then the actions and their logic are lost)

The system currently is missing a proper way to handle contextual sprites for buttons. The old system *technically* did have one, but it was for a very specific component.