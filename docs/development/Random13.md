# Random13

In order to support demo views in the future, create consistent behavior and minimise cheating; Random13 exists to provide a deterministic approach to random chance for Unitystation. 

This class should generally be used on the server only. For any random chances that involves the client, use `DMMath.Prob()`

# Using the random table.

Random13's `Prob()` function uses a variation of [Doom's Pseudorandom number generator](https://doomwiki.org/wiki/Pseudorandom_number_generator) which has proven to be quite robust despite how simple it is. There's always a 50/50 chance that you'd get true or false and the results will always stay consistent when re-tracing steps.

This function triggers the `OnRandomTableIndexChanged` event, which can be handy for keeping objects up to date with specific info when the table's index advances.

``` cs

//example
//component for felines that randomly heals 25 damage every time they get lucky and the random table advances to a specific lucky number.
public class LuckyCat : Monobehavior 
{
    public LivingHealthMasterBase health;

    private void Awake()
    {
        GameManager.Random.OnRandomTableIndexChanged += HealingPurr;
    }

    private void HealingPurr(int currentNumber)
    {
        if (currentNumber == 3) health.OrNull()?.Heal(25);
    }
}
```


!!! note

    Do not use this while iterating through large amount of tasks/objects.

# Using Time for randomness.

Random13 provides a second way to provide deterministic behavior and it is via the `ProbFromTime()` method. It is generally an excellent way to have consistency when dealing with systems that take time into consideration, and is technically more random than `Prob()` due to its infinite nature.

Because this function relies on time to work, it is recommended to keep a consistent DateTime variable like the one inside the `GameManager` to ensure maximum reliability.