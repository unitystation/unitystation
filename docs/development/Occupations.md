This page briefly describes how occupations (sometimes called "jobs") are defined.

All of the possible occupations in the game are represented using an `Occupation` Scriptable Object. These currently live in Resources/ScriptableObjects/Occupations . Refer to the tooltips on the Occupation SO for details on what each field does.

Among other settings, each Occupation references an inventory populator, which defines what their starting items will be. You can find these at Resources/ScriptableObjects/Inventory/Populators/Storage/Occupations.

The list of currently selectable occupations lives in the AllowedOccupations OccupationList SO, at Resources/ScriptableObjects/Occupations. This also defines the order the occupations show up in in the occupation selection screen.