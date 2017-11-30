**UnityStation: Power cable design proposal**

**Placement of cable tiles:**
- Every cable is a ConnectedTile, changing sprite to connect to the tiles around it.

- Every Cable can only connect to a cable of the same colour

- Cables with different colours can connect by a &quot;connector box&quot; which has a Gui to select the colours to connect.

**Determination of Power Networks:**
- A powernetwork, consist of a network of ConnectedCableTiles

- On connect, they form a &quot;PowerNetwork&quot;

- If one or more cables already have a powernetwork, they would all switch to use the lowest number powernetwork available. (PN1 and PN2 are joined, both are now PN1)
- The powernetwork setting propagates through all powercable tiles that are in the network.

**The determination of power:**
- Every PowerNetwork has a &quot;PowerPotential&quot; which is the total of all connected poweroutputs combined
- Every PowerNetwork has a &quot;PowerRequested&quot; which is the total amount of power requested
- Every PowerNetwork has a &quot;PowerUsed&quot; which is the total amount of power being used.
- every PowerNetwork has a &quot;PowerAvailable&quot; which consists of PowerPotential minus Power PowerUsed

**Connecting Devices:**
- Every connected device, has &quot;DevicePowerRequested&quot; and &quot;DevicePowerUsed&quot;
- Every connected device calculates DevicePowerUsed, relative to the networks &quot;PowerRequested&quot;, &quot;PowerPotential&quot; and its own &quot;DevicePowerRequested&quot;


**Concussion:**
The above system, creates a more intuitive way of placing cables and gives players the potential to create more complex power systems by combining cables of different colors.

Also it seems relatively easy to implement and edit on the coding side of things.