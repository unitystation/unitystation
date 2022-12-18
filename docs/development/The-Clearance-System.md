# What is the Clearance System?

In simple terms, the clearance system is just another system that other pieces of code can consume in order to put functionality behind a clearance check. Clearance representing the level of access the performer of an action has at the moment of requesting it. The clearance system is a way to make sure that only people with the right clearance can do certain things.

# How does it work?

The clearance system is pretty simple, it is composed of 3 main parts:

1. The **clearance definition**. An enum located at ``Systems/Clearance/Clearance.cs`` that defines the different levels of clearance, or clearance for different actions/rooms.
2. A **clearance source**. A class that implements the ``IClearanceSource`` interface and represents the source of the clearance. This can be a mob, a player, an item, etc. A good example of a clearance source is the ID card. It has a list of clearances.
3. A **clearance restricted**. An object that has the ``ClearanceRestricted`` component attached to it. This component has clearance requirements for normal and low pop rounds. It also has a ``CheckType``, which determines the strategy that will be used to compare the source with the requirements.

# How do I make an object clearance restricted?

1. Add the ``ClearanceRestricted`` component to the object.
2. Set the ``TypeCheck`` to the strategy you want to use. It could be ``Any`` or ``All``, so it checks if any of the requirements are met or if all of them are met respectively.
3. Implement an interaction from which you will get an ``IClearanceSource``. Most of the time this will be an ID card you can grab from the used object during the interaction.
4. Call ``ClearanceRestricted.HasClearance()`` passing the ``IClearanceSource`` you got from the interaction. This will return a boolean indicating if the source has the required clearance.

# Implementation example

For this example we made a simple Terminal that you can swipe an ID card on and it will tell you a joke if you have the right clearance. The terminal is clearance restricted, so you can't just swipe any ID card on it. You need to have the ``ClownOffice`` clearance.

## The item

The item is a simple prefab with the ``ClearanceRestricted`` component attached to it. The ``TypeCheck`` is set to ``Any`` and the clearance requirements are set to ``ClownOffice``. This means that the terminal will only accept ID cards that have the ``ClownOffice`` clearance, but ignore if the ID has any other clearances.

## The interaction

We create another component and call it ``JokeTerminal``. This component will have a reference to the ``ClearanceRestricted`` component and will implement the ``IInteractable`` interface. This way we can add the interaction to the terminal.

```csharp
public class JokeTerminal: MonoBehaviour, IInteractable<HandApply>
{
    //Internal reference to cached clearance restricted component
    private ClearanceRestricted clearanceRestricted;

    private void Awake()
    {
        //Cache the clearance restricted component
        clearanceRestricted = GetComponent<ClearanceRestricted>();
    }

    public void ServerPerformInteraction(HandApply interaction)
    {
        //Check if the interaction has an ID card
        if (interaction.UsedObject.TryGetComponent<IDCard>(out var idCard))
        {
            //Check if the ID card has the required clearance. IdCard compoment has a reference to an IClearanceSource
            if (clearanceRestricted.HasClearance(idCard.ClearanceSource))
            {
                //Tell the player a joke
                Chat.AddExamineMsgFromServer(interaction.Performer, "Knock knock");
            }
            else
            {
                //Tell the player they don't have the clearance
                Chat.AddExamineMsgFromServer(interaction.Performer, "You don't have the clearance to use this terminal");
            }
        }
    }
}
```

# Dos and Don'ts

1. Use the ``ClearanceRestricted`` component to make an object clearance restricted. **Never ever** implement checking clearance yourself in your component. Grab a reference to the ``ClearanceRestricted`` component and use it to check clearance.
2. Use the ``BasicClearanceSource`` component to make an object a clearance source, unless you have a very good reason to implement your own ``IClearanceSource``.
3. You can use the ``PerformWithClearance`` convenience method from ``ClearanceRestricted`` if your use case is simple enough. This method receives an ``IClearanceSource`` and two actions, one for when the clearance is met and one for when it isn't. This method will run the success or failure action accordingly.
4. You can use the ``HasClearance`` overload from ``ClearanceRestricted`` that receives a ``GameObject``. This method will try to get an ``IClearanceSource`` from the object and then check if the source has the required clearance. This is useful if you want to check clearance but don't know where the source could be (not in hand, not in a specific slot, etc.). It also works with mobs that could be an ``IClearanceSource``. This method is quite expensive, so use it only when you have to.