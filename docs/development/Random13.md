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

# Cheating

`Prob()` has 255 predictable states, ProbFromTime has an infinite amount of states that is always changing all the time.
255 always changing states is pretty hard to brute force in a game like Unitystation where there are a lot of changing elements that cause the random table to constantly move all the time, but it's still possible to predict a desired result if someone finds the current index from a pattern that the server is on via observation.
`ProbFromTime()` is much harder to predict and use to your advantage while you're playing, it is best used when you're having randomness in things that involve points, events or things that has a positive advantage for players.
