# Hunger Explained

## Overview

The hunger system in UnityStation is a modular living-creature system that tracks nutrient demand, distributes blood-borne nutriment to body parts, applies hunger states, and triggers alerts and status effects.

The hunger system manages:

- registered `HungerComponent` body parts
- the mapping of nutriment reagents to consuming body parts
- per-tick hunger evaluation and state changes
- alert and status effect updates.

## Key components

### HungerSystem

`HungerSystem` is the central manager.

Responsibilities:

- collect all `HungerComponent` body parts via `BodyPartAdded` / `BodyPartRemoved`
- build `NutrimentToConsume` from registered body parts
- initialise hunger rates and stored fat via `StartFresh` / `InitialiseHunger`
- run `SystemUpdate` each tick
- choose a hunger calculation strategy through `HungerCalculationMethod`
- update status effects when the hunger state changes.

Important fields:

- `NutrimentToConsume`: A dictionary that maps what reagents body parts consume/release.
- `BodyParts`: list of all active `HungerComponent` components.
- `BodyNutriment`: default nutrient reagent assigned to unconfigured body parts.
- `NumberOfMinutesBeforeStarving`: target starvation window used when initialising fat stores
- `CashedHungerState`: last applied hunger state for change detection. Default is usually `Normal`.
- `HungerCalculationMethod`: runtime-selected `IHungerCalculation` implementation.

### HungerComponent

Each body part that participates in hunger has a `HungerComponent`.

This component mainly exists as a data container, and should be used as such.

Key behavior:

- stores the part's `Nutriment` reagent type
- stores a passive consumption rate in `PassiveConsumptionNutriment`
- stores a `HungerModifier` that affects part efficiency
- tracks the part's `HungerState`
- computes healing from nutriment via `NutrimentHeal`
- derives blood throughput from its attached `ReagentCirculatedComponent`.

`HungerComponent` is attached to individual body parts and is what `HungerSystem` registers and updates.

### IHungerCalculation

`IHungerCalculation` is the hunger-state strategy interface.

Implementations compute a `HungerState` for the creature each update by inspecting `LivingHealthMasterBase` and the `HungerSystem`.

This design allows different species to have their own hunger calculation method, and allows unique GameModes and rules to replace hunger behaviors during runtime.

### DefaultUnityStationHungerCalculation

This is the primary UnityStation-style hunger algorithm.

Workflow:

1. Sum heart output from `creatureHealth.reagentPoolSystem.PumpingDevices`
2. Call `NutrimentCalculation(...)` with that efficiency and the current `NutrimentToConsume`
3. Return the creature's overall hunger state by scanning all registered body parts via `CheckHungerStateOnAll(...)`

`NutrimentCalculation` looks at all the body parts during the calculation, and decides if its hunger state is `Starving` or `Normal`.

`CheckHungerStateOnAll` checks for the lowest state a body part has, and it bases the entire creature's hunger state based on that. So if a single body part is starving, it decides that the creature is starving despite having 20 other well fed body parts.

### BodyFat and Stomach

The hunger system also interacts with organs that represent stored energy.

#### Stomach

`Stomach` digests its `StomachContents` into the blood pool each `LivingHealthMasterBase` tick:

- it consumes `DigesterAmountPerSecond * RelatedPart.TotalModified`
- converts that reagent mix into blood using `ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add`
- if the stomach is empty, it sets its own `HungerComponent.HungerState = Starving`
- if the stomach is near full, it sets its own `HungerComponent.HungerState = Full`
- otherwise it sets its own state to `Normal`
- when all associated `BodyFat` stores are full, it spawns additional fat and reports weight gain.

#### BodyFat

`BodyFat` represents stored nutriment.

During `ImplantPeriodicUpdate`: 

- if blood nutriment percentage is below `ReleaseNutrimentPercentage`, it releases stored nutriment into blood
- if blood nutriment is high and the fat store is not full, it absorbs nutriment from blood
- if stored fat exceeds `DDebuffInPoint`, it applies movement speed debuffs via `IMovementEffect`
- it uses `AbsorbedAmount` to derive its own `HungerComponent.HungerState`:
  - `0` → `Malnourished`
  - `< 5` → `Hungry`
  - `> NoticeableDebuffInPoint` → `Full`
  - else → `Normal`

This means fat stores are both an energy reserve and a separate contributor to hunger state on the stomach/fat body part.

## Runtime flow

### Registration and initialisation

- When a body part with a `HungerComponent` is added, `HungerSystem.BodyPartAdded` registers it and calls `BodyPartListChange()`.
- `BodyPartListChange()` rebuilds `NutrimentToConsume` by grouping body parts by `bodyPart.Nutriment` and summing their demand.
- `StartFresh()` assigns `BodyNutriment` to any body parts missing a nutrient reagent, then calls `InitialiseHunger(...)`.
- `InitialiseHunger(...)`:
  - computes total blood throughput across all hunger body parts
  - sets each part's `PassiveConsumptionNutriment` so the whole creature consumes ~1 unit of nutriment per minute
  - scales all stomach `BodyFat.AbsorbedAmount` values so the creature lasts roughly `NumberOfMinutesBeforeStarving`
  - applies a ±25% random variance to stored fat so starvation timing differs between creatures
  - calls `BodyPartListChange()` again and updates effects to `Normal`.

### Per-tick update

`SystemUpdate()` runs each mechanical tick.

- it calls `HungerCalculationMethod.CalculateHungerState(Base, this)`
- if the returned state differs from `CashedHungerState`, it calls `UpdateStatusEffects(...)`
- `UpdateStatusEffects(...)` removes the old status effect and applies the new one via `StatusEffectManager`

## Nutriment consumption logic

This is the core of the current hunger simulation.

### Body part demand calculation

Each `HungerComponent` contributes demand based on:

- `PassiveConsumptionNutriment` (baseline metabolic consumption)
- `BloodThroughput` from the `ReagentCirculatedComponent`

These are aggregated in `NutrimentToConsume[reagent].TotalNeeded`.

### Heart efficiency and delivery

`DefaultUnityStationHungerCalculation` uses two limits:

- `heartEfficiency`: sum of `CalculateHeartbeat()` from all pumping devices
- `availablePercentage`: the fraction of the required nutriment currently present in `health.reagentPoolSystem.BloodPool`

The effective delivery fraction is:

- `effective = Mathf.Min(HeartEfficiency, availablePercentage)`

This means the actual delivery is limited by circulation and by available blood nutriment.

### Blood pool removal

For each nutrient reagent type:

- compute `needed` as the total baseline demand
- add extra demand for damaged body parts using `HealingNutrimentMultiplier`
- remove `amount = needed * effective` from `BloodPool`

### Body part state updates

For each related body part:

- if `effective > 0.1`:
  - set `HungerModifier.Multiplier = 1`
  - set `HungerState = Normal`
  - if the part is damaged, apply healing via `NutrimentHeal(...)`
- else:
  - set `HungerModifier.Multiplier = 0.5`
  - set `HungerState = Starving`

Damaged body parts increase nutrient demand and may be healed when sufficient delivery is available.

### Overall creature hunger state

`CheckHungerStateOnAll(...)` determines the final state from all registered `HungerComponent`s:

- if any body part is `Full`, the whole creature is `Full`
- otherwise the creature takes the worst state seen across parts
- if `Starving` is reached, the check stops early

This means a single full body part can mark the creature as full, while starvation anywhere propagates to the whole creature if no full part exists.

## HungerState values

The `HungerState` enum maps the following states:

- `Full`
- `Normal`
- `Hungry`
- `Malnourished`
- `Starving`

`HungerSystem.GetAlertSOFromHunger(...)` maps several of these states to HUD alerts, while `GetStatusEffectFromHunger(...)` maps them to status effects.

Current mappings:

- `Full` → fat alert / `FatStatusEffect`
- `Normal` → no alert, `NotHungryStatusEffect`
- `Hungry` → hungry alert / `HungryStatusEffect`
- `Malnourished` → malnourished alert / `NotHungryStatusEffect`
- `Starving` → starving alert / `StravingStatusEffect`