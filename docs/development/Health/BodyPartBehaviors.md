# Body Part behaviors

Body part functionality lives in serializable C# objects stored in a `BodyPartBehaviors` component's `Behaviors` list. For example, `Heart` and `Bones` are entries in that list rather than separate Unity components.

`BodyPartBehaviors` use Interface Selectors to manage and update behaviors a body part can have during runtime. This means that body parts can implement any custom behavior that uses a `IBodyPartBehavior` during runtime.

## Host, contract, and base class

| Type              | Responsibility                                                                                                                                                     |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| BodyPartBehaviors | The Unity `NetworkBehaviour` that stores behaviors and forwards Unity lifecycle, inventory, EMP, and networking events. Only one is allowed per GameObject.        |
| IBodyPartBehavior | The contract for initialization, body attachment/removal, periodic updates, damage, and system setup.                                                              |
| BodyPartBehavior  | A plain, serializable base class implementing that contract, with optional hooks and access to the owning component. Most new behaviors should inherit this class. |

`BodyPartBehavior.Owner` is the host component. `RelatedPart` is initially found with `Owner.GetComponentInParent<BodyPart>()`; `LivingHealthMaster` reads that part's current `HealthMaster`. 

*Note: The host can also support ordinary item behaviors, so a behavior must not assume that every host has a body part or health master.*

The base class exposes `gameObject`, `transform`, component lookup helpers, coroutines, and a destruction cancellation token through its owner. These are services of the host: a behavior does not receive Unity messages such as `Awake` or `Update` by itself. Override the behavior hooks instead.

## Configuring a prefab

1. Put `BodyPartBehaviors` on the body part's GameObject. Its network context must have a valid `NetworkIdentity`, as required by Mirror.
2. Add implementations through the Inspector's `Behaviors` implementation picker.
3. Configure each entry's serialized fields and add its sibling dependencies.
4. Order entries whose attachment callbacks depend on another entry's completed attachment. For example, put `ReagentCirculatedComponent` before `Heart` so its `AssociatedSystem` is available when the heart registers itself.
5. Check prefab variant overrides when changing an inherited setup.

The list uses `[SerializeReference]`, preserving concrete behavior types and shared managed references within the host. Mark each concrete behavior `[Serializable]`. Use `[SerializeReference]` for serialized links between managed behaviors, including polymorphic lists and shared references.

Managed references do not provide shared object identity across separate Unity serialization hosts. For a reference to another prefab, store a Unity object reference, such as a `GameObject`, and find its behavior when needed. Do not serialize a prefab's behavior object into another host as a substitute for a prefab reference.

## Lifecycle and execution order

The host initializes in two passes: it calls `Initialize(this)` on every entry, then calls every entry's `OnInitialize()`. Thus sibling owners are bound before initialization callbacks begin, but a sibling's own initialization callback may not have run yet. Cache dependencies here; wait for body attachment before using body-wide systems.

| Hook                                           | Current caller and purpose                                                                                                                                                 |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Initialize(owner)`                            | The host's idempotent initialization binds the owner and related part. Inheriting the base class supplies this implementation.                                             |
| `OnInitialize()`                               | Called once by the host after binding all entries. Cache sibling behaviors and native components here.                                                                     |
| `OnBehaviorEnabled()` / `OnBehaviorDisabled()` | Forwarded from the host's Unity enable/disable callbacks for enabled entries. Pair subscriptions and unsubscriptions here where appropriate.                               |
| `OnBehaviorStarted()`                          | Forwarded from the host's Unity `Start` for enabled entries.                                                                                                               |
| `SetUpSystems()`                               | `BodyPart.SetUpSystemsThis()` calls this once per body part, guarded by its `SystemSetup` flag. It runs after assigning the health master and before attachment callbacks. |
| `OnAddedToBody(livingHealth)`                  | Called by `BodyPart` when attaching the part. Register with body-wide systems here.                                                                                        |
| `ImplantPeriodicUpdate()`                      | Called directly by `BodyPart` during its periodic update, in reverse `OrganList` order.                                                                                    |
| `OnRemovedFromBody(livingHealth, source)`      | Called while the part still has its health master. Remove registrations and use the supplied health master for cleanup.                                                    |
| `OnTakeDamage(data)` / `InternalDamageLogic()` | Available through the host's explicit forwarding methods. Damage subscribers can also receive events from `BodyPart`; the hooks are not automatic Unity messages.          |
| `OnBehaviorDestroyed()`                        | Called for all non-null entries when an initialized host is destroyed. Release remaining subscriptions and resources.                                                      |

`BodyPart` also registers attached behaviors in the health master's organ lookup, including their behavior-derived base types. This allows body-wide code to find organs by type without requiring Unity component inheritance.

### Current scheduling and enable-state limits

- `BodyPart.Awake()` copies the host's list into its own `OrganList`. Editing `Behaviors` at runtime does not automatically refresh that cache or initialize newly inserted entries.
- Organ ticking is driven by `BodyPart`, not a second host timer. Do not register another periodic callback merely to invoke the same organ update again.
- `SecondsUntilNextTick` is currently declared on the host but is not used to schedule organ updates.
- `Enabled` filters the host's forwarding paths. The direct `BodyPart` loops for periodic updates, system setup, and attachment/removal do not check it. It is therefore not a universal switch for stopping an organ's logic.
- Initialization runs for all non-null entries, including disabled ones. Changing the `Enabled` field itself does not invoke enable/disable hooks.

## Looking up dependencies

Use BodyPartBehaviorExtensions to find managed behaviors. Use Unity component lookup for actual components.

```csharp
// From a GameObject or Component:
Heart heart = bodyPart.GetBodyPartBehavior<Heart>();
bool hasBones = bodyPart.TryGetBodyPartBehavior<Bones>(out var bones);

// Inside a BodyPartBehavior:
var circulation = GetBodyPartBehavior<ReagentCirculatedComponent>();
var storage = GetComponent<ItemStorage>();
```

`GetBodyPartBehavior<T>()` returns the first matching entry on the same GameObject's host. It initializes scene-instance hosts on demand, but does not initialize prefab assets just to inspect their configuration. The plural and child lookup helpers return matching entries without that initialization step.

`GetComponent<Heart>()` and `AddComponent<Bones>()` are inappropriate because these classes are managed objects. For interfaces implemented by both components and behaviors, use `GetComponentOrBodyPartBehavior<T>()` or `GetComponentsAndBodyPartBehaviors<T>()`.

## Adding a behavior

This minimal example follows the same dependency lookup and periodic-update pattern. It reads blood volume after circulation has been associated with a body.

```csharp
using System;
using US13.HealthV2.Living;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;

namespace US13.Items.Implants.Organs
{
    [Serializable]
    public sealed class BloodLevelBehavior : BodyPartBehavior
    {
        private ReagentCirculatedComponent circulation;
        public float BloodFraction { get; private set; }

        public override void OnInitialize()
        {
            circulation = GetBodyPartBehavior<ReagentCirculatedComponent>();
        }

        public override void ImplantPeriodicUpdate()
        {
            var system = circulation?.AssociatedSystem;
            if (system == null || system.StartingBlood <= 0) return;
            BloodFraction = system.BloodPool.Total / system.StartingBlood;
        }
    }
}
```

Add this implementation and `ReagentCirculatedComponent` to the same host. Its private dependency field is a runtime cache; it is resolved during initialization.
The example does not synchronize `BloodFraction` to clients, as all logic runs by default on the server side only unless stated otherwise in the body part code.

## Networking

The host retains Mirror's component lifecycle and network identity. Individual behaviors use RpcMessageQueue for synchronized state and targeted events rather than declaring `[SyncVar]`, `[ClientRpc]`, or `[TargetRpc]` members on a plain class.

For a behavior inheriting `BodyPartBehavior`:

1. Override `CaptureNetworkState()` to return a JSON snapshot of the state clients need. The default `null` means there is no synchronized state.
2. Override `ApplyNetworkState(json)` to apply received values and any required client-side effects. Assigning a plain field has no automatic Mirror hook.
3. Set `OwnerOnlyNetworkState` when snapshots should reach only the identity's owning connection.
4. For targeted events, call `Owner.SendEvent(this, eventName, payload, recipient)` on the server and handle them in `OnNetworkEvent()`.

The server registers a ticket using the host's network ID and component index. 

Clients subscribe and request an initial snapshot. The server checks snapshots every 0.1 seconds and publishes changed results per observer, with separate owner and observer views. Initial requests require the requesting connection to observe the identity. Host clients share server instances and targeted local events are delivered directly.

Snapshots and events address behaviors by their list index, so server and client prefab configurations must agree on list order. Network registration currently starts only if at least one entry returns a non-null snapshot; an event-only behavior must account for that gate. Networking hooks are supplied by the base class, not by `IBodyPartBehavior` alone.