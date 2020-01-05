# Item Traits System

This page briefly describes the Item Traits system. When we say "item" we usually mean "things you can pick up", but I think we would also use them for objects (things you can't pick up, like canisters, machines, etc...)

An ItemTrait is an instance of the `ItemTrait` Scriptable Object (or a subclass).

Each object can have one or more ItemTraits assigned to it, via the ItemAttributes component.

Currently, these are used in the Inventory System to define what is allowed to go in a given object's item slots. These are also used to determine whether certain interactions can occur (for example, a canister checks if the object being used on it has the "Wrench" ItemTrait). 

Any traits which need to be frequently referenced throughout the codebase can be added to the CommonTraits singleton SO to avoid having to assign them to each individual component that wants to use them.

All ItemTrait assets currently live in Resources/ScriptableObjects/Traits.