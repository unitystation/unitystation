# Development Standards Guide

Having a consistent code style and basic standards for code quality helps to keep our code "clean", making it easier to develop as our project grows. It would be awesome to build a game as complex as SS13 that is also easy to keep improving upon. Remember that each line of code you add now will be read by many other fellow developers in the future (including you!), so writing code that's easy to understand has a very positive impact on the project. 

These are not strict rules, and reviewers don't expect developers to be able to follow all of these suggestions, but we strive to adhere to them as much as possible. When a reviewer looks at your PR, part of their review will involve thinking about code cleanliness. They will work with you to figure out if / what changes can be made to the PR to help with code cleanliness. They might suggest some items from this list or something from their own experience. As you submit more and more PRs, the reviewers you collaborate with should help you develop a better sense of how to write clean code until it becomes second nature.

As a supplement to this, you can check out some best practices for Unity development here: [50 Tips and Best Practices for Unity](https://www.gamasutra.com/blogs/HermanTulleken/20160812/279100/50_Tips_and_Best_Practices_for_Unity_2016_Edition.php)

And we highly recommend that you watch this video as it covers a lot of common mistakes that developers do: [20 Advanced Coding Tips For Big Unity Projects](https://youtu.be/dLCLqEkbGEQ)

## Code Style

1. All classes, public methods and public fields should have XML `///` comments. Parameter and return value documentations should be descriptive. Class documentation should describe the purpose of the class. Also comment any code where it might not be obvious to someone else why it's doing something. You can read more about how to write XML comments [here](https://docs.microsoft.com/en-us/dotnet/csharp/codedoc).
For example, here's a well-documented method: 

        :::csharp
        /// <summary>
        /// Trigger an interaction with the position set to this transform's position, with the specified originator and hand.
        /// </summary>
        /// <param name="originator">GameObject of the player initiating the interaction</param>
        /// <param name="hand">hand being used by the originator</param>
        /// <returns>true if further interactions should be prevented for the current update</returns>
        public bool Interact(GameObject originator, string hand)

2. Usually, use a separate .cs file for each public class or struct. It is sometimes preferable to have more than one in the same file, but if uncertain err on the side of using a separate file.

3. Use **P**ascal**C**ase for all file names, functions and public variables. 

3. Use namespace declarations matching the script's folder path on all new or modified script files.

        :::csharp
        namespace SampleNamespace
        {
            class SampleClass
            {
                public void SampleMethod()
                {
                     System.Console.WriteLine(
                        "SampleMethod inside SampleNamespace");
                }
            }
        }


3. All variables and non-public fields except for constants / constant-like ones should use camelCase (with no prefix letter).
        
        :::csharp
        public class Greytider
        {
            private float rage;
            
            public bool Grief(Player griefingVictim)
            {
                float desireToGrief = griefingVictim.griefability - griefPoints / 100
            }
        }  

3. All constants or constant-like variables should use uppercase snake case - "CONSTANT_NAME"
    
        :::csharp
        public class ExplodeWhenShot
        {
            private const string EXPLOSION_SOUND = "Explode01.wav";

            //this is not an actual constant, but is initialized in Start() and never modified
            //afterwards so it should still be named using this convention
            private int DAMAGEABLE_MASK;

            private void Start()
            {
                DAMAGEABLE_MASK = LayerMask.GetMask("Players", "Machines", "Default");
            }
        }


3. Properties, public fields, classes, structs, enum constants, and methods should use PascalCase - "SomeName".
            
        :::csharp
        public struct PlayerState

        public class PlayerSync
        {
            public bool IsGhost;
            public bool IsAlive { get; set; }
            public void Attack(GameObject attacker)
            {
            ...
            }
        }    

        public enum DamageType
        {
            Poison,
            Fire,
            Brute
        }    

3. We tend to prefix variables and methods with underscores to indicate that you shouldn't use them unless you REALLY know what you are doing and understand the implications, like `Spawn._ServerFireClientServerSpawnHooks`.
3. Any TODO comments added in a PR should be accompanied by a corresponding issue in the issue tracker. A TODO comment means that there's something we need to remember to do, so we need to put it in the issue tracker to make sure we remember.
3. Script folders should have no more than 10 scripts in a given folder. Reorganize, refactor, or add subfolders as needed to avoid folders getting too large.
3. Local variables should be declared near where they are used, not at the beginning of their containing block.
            
        :::csharp
        public float LongMethod()
        {
        //Don't declare it here, it's not used until later!
        //float distance;
        ...do a bunch of stuff...
        ...do a bunch of stuff...
        ...do a bunch of stuff...
        //declare it here instead!
        float distance = CalculateDistance();
        bool isInRange = distance > MAX_RANGE;
        }
            
3. When checking if something is null, use `something == null` rather than `!something` - it's easier for newer devs to understand.
3. When checking if a boolean is false, use `someBool == false` rather than `!someBool` - sometimes it's easy to miss that exclamation point.  
  
3. Format code according to our standard formatting style. Your IDE will be able to apply some of these automatically by reading this from our .editorconfig file. For reference (or in case .editorconfig can't support these), here's the style conventions:  
    * Indent using tabs rather than spaces.
    * When checking in code, try to ensure it uses Unix-style (LF) line endings. If you're on OSX or Linux, you don't need to do anything. If you are on Windows, ensure that you have configured git to check out Windows style but commit Unix-style line endings using the command `git config --global core.autocrlf true` or configuring this using your git GUI of choice.

    * Avoid long lines. Break up lines of code that are longer than 120 characters. Alternatively, try to refactor the statement so it doesn't need to be so long!  
            
            :::csharp
            //too long!
            float distance = Vector3.Distance(Weapon.Owner.transform, PlayerManager.LocalPlayer.gameObject.transform) + blah blah blah.

            //better!
            float distance = Vector3.Distance(Weapon.Owner.transform, 
                PlayerManager.LocalPlayer.gameObject.transform) + blah blah blah.

            // best! Refactor into a private method
            float distance = DistanceToLocalPlayer(Weapon) + blah blah blah 

    * Curly braces should always be on a line by themselves:  
                
            :::csharp
            if (condition)
            {
                ....
            }
            else
            {
                ....
            }


## Component and Scriptable Object Design
1. If a field should be assignable in editor, make it private and add the `[SerializeField]` attribute and a `[Tooltip("<description>")]`. If other classes must be able to read the field, use a getter-only auto property. This improves the encapsulation of your class, preventing other components from potentially breaking your class. **Don't trust that others will know they aren't supposed to modify your field!**:
        
        :::csharp
        public class Health : MonoBehavior
        {
            [Tooltip("initial HP of this character)]
            [SerializeField] private float initialHP;
            ///<summary>
            ///The initial HP of this character.
            ///</summary>
            public float InitialHP => initialHP;
        }

2. If you are SURE that you want the field to be modifiable by other classes, you can turn the getter into a property so you can add logic to validate / react to when the value is changed by other classes. 
**You should usually not have a public field without any setter logic**, as in most cases your component
will need to perform some logic when the value is changed. If you really don't need any setter logic, it's okay
to just leave it as a public field without a property. As always, make sure to document the effect of getting / setting.  
        
        :::csharp
        public class Health : MonoBehavior
        { 
            [SerializeField] private float initialHP;
            ///<summary>
            ///The initial HP of this character. Setting this
            ///will cause the character's HP to be higher upon respawn.
            ///</summary>
            public float InitialHP 
            {
                get => initialHP;
                set
                {
                    //custom setter logic here
                    initialHP = value;
                }    
            }
        }
          

3. Public methods which only function correctly on the server must be prefixed with "Server" (or at least include Server somewhere in the name). It makes the name longer but it saves developers the trouble of wondering whether it's okay to call the method from the clientside, which can prevent quite nasty bugs. Do not rely on the `[Server]` attribute, since it's only available on NetworkBehavior components and is not visible in autocomplete. It's less vital but still recommended to follow this convention on private methods.

## Code Design
1. Follow the philosophy "tell, don't ask". TELL a class to do something rather than ASKING it for some data and operating on that data. Treat a class as a folder for grouping together data and the logic that operates on that data.

        :::csharp   
        //bad - ASKing for health, modifying the health outside of the Player class
        var health = player.Health;    
        health = player.Invincible ? health : health - weapon.damage;
        Player.Health = health;

        //good - TELL the Player to handle an attack made against them
        player.Attack(weapon);
        
3. Avoid many levels of nested indentation, almost certainly no more than 7, preferably no more than 3. You can solve this by taking deeply-nested logic and putting it into a descriptively-named private method.
        
        :::csharp
        //bad
        if (test1)
        {
            if (test2)
            {
                if (test3)
                {
                    if (test4)
                    {
                    ...
                    }
                }
            }
        }

        //better
        if (test1)
        {
            if (test2)
            {
                AdditionalLogic();
            }
        }

        private void AdditionalLogic()
        {
            if (test3)
            {
                if (test4)
                {
                ...
                }
            }
        }  
        
4. Try to keep individual .cs files small. Shoot for less than 500 actual lines of code (ignoring comments / blank lines). You can use refactoring, design patterns, and other techniques to try to make them small by splitting logic up into other .cs files / classes.
5. When deciding what type to use, strings should be used only as a last resort. Prefer other types, such as enums, numeric types, custom classes, etc...if they are more appropriate.
6. Most string or numeric literals should be constants.
7. Don't define constants that have the same value in multiple places. There should only ever be one place you need to change if you ever need to change a constant's value.
        
# Writing efficent and maintainble code

Unitystation is a giantic and ambitous project that holds a lot of content and systems that can get tangled together, to avoid this; we highly recommend that you write your code in a modular way that's easy for developers to maintain and and expand upon.

Unity has a great talk on how to do just that in this video: [How to adopt a modular code workflow for your team | Unite 2022](https://youtu.be/nk3gHIZZ5Rg)

Below are good practices and explainations on what to do, so you can avoid having your code refactored/reworked 5 months later and make the lives of other contributors much easier.

## 0. In-code documentation

* You can avoid confusing others (and even yourself) by naming your functions and variables in a clear and descriptive way.

```cs
//bad code
void doThing(bool b)
{
    state = b;
}
```

```cs
//good code
private void ThisFunctionWillDoA_B_And_C(bool newState)
{
    state = newState;
}
```

* Explain what your function does step by step whenever possible. Your code might seem obvious to you, but for other people; that may not be the case.

```cs
//Example from NightVisionGoggle.cs

/// <summary>
/// Checks if the item is in the correct ItemSlot which is the eyes.
/// Automatically returns false if null because of the "is" keyword and null propagation.
/// </summary>
private bool IsInCorrectNamedSlot()
{
	return pickupable.ItemSlot is { NamedSlot: NamedSlot.eyes };
}

private void ApplyEffects(bool state)
{
	var finalState = state;
	// If for whatever reason unity is unable to catch the correct main camera that has the CameraEffectControlScript
	// Don't do anything.
	if (Camera.main == null ||
		Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return;
	// If the item is not in the correct slot, ensure the effect is disabled.
	if (IsInCorrectNamedSlot() == false) finalState = false;
	// Visibility is updated based on the on/off state of the goggles.
	// True means its on and will show an expanded view in the dark by changing the player's light view.
	// False will revert it to default.
	effects.AdjustPlayerVisibility(finalState);
}
```


* Make proprer use of `Logger`

It's easy to debug problems when the game tells us that there is one. As a result, we use `Logger`; a logging system that allows us to store records of how the game has been behaving/misbehaving while it's running.

Logger should only be used to report unintended behaviors, errors and backend system messages. Never spam logger and remove any debugging messages you have left once you're done using it to print testing values/results in the console.

To make our lives easier as well, add the class/gameObject name and function in the log message so it's easier to tell ourselves what went wrong from a surface.

_Note: Avoid using unity's `Debug` class for logging as it doesn't store logs where we need them to be._
```cs
//bad use of logger.

Logger.Log("something happened");
```
```cs
//good use of logger.

Logger.Log("[Manager/OnRoundEnd/Cleanup] - Manager cleaned references succesfully.");
Logger.LogError($"[Item/ServerPerformInteraction] - Tried applying item on {gameObject.name} but it was missing X!");
Logger.LogWarning("[Object/OnEnable] - Integer values appear to be too high, unintended behavior may occur.");
```


## 1. Utilise Events and Minimise Dependencies

* Video games are event driven, you should use the `UpdateManager` as sparingly as possible and when it actually makes sense to constantly update a state of an object every frame.
* Make use of `Action<>` and `UnityEvent<>` whenever you want to tell an object to do something based on a change or function being called on something else.

_For more info about UnityEvents check: [The Offical Unity Docs](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html)_

* Events can be used to reduce the likelyhood of bugs and [NREs](https://learn.microsoft.com/en-us/dotnet/api/system.nullreferenceexception?view=net-7.0) occuring and makes our code much more maintainable and easy to expand upon due to how they're designed.

With events, components don't have to constantly reference each other and only require a subscription to relevent functions that get invoked when things happen in the game.

```cs
//example of bad code

class Health : Monobehavior
{
    public bool StateChanged;
    public int currentHealth = 5;

    void stateChange(int damageTaken){
        currentHealth -= damageTaken;
        if(currentHealth >= 0) return;
        StateChanged = true;
        Inventory.DropAll(); //How do we know that we have an inventory at all?
    }
}

class DeathUI : Monobehavior
{
    public Health health; //<- possibily null

    void Awake(){
        UpdateManager.Add(FIXED_UPDATE, myFunc);
    }

    void myFunc(){
        if(health.StateChanged == false) return;
        DoThing();
    }
}

class PlayerInventory : Inventory{
    public void DropAll();
}
```

```cs
//example of good code

class Health : Monobehavior
{
    public int CurrentHealth {private set; get;} = 5;
    public Action OnDeath = new Action();

    public void TakeDamage(int damageTaken)
    {
        currentHealth -= damageTaken;
        if(currentHealth >= 0) return;
        OnDeath?.Invoke();
    }
}

class DeathUI : Monobehavior
{
    void OnEnable()
    {
        if(gameObject.TryGetComponent<Health>(out var playerHealth) == false) 
        {
            //Always leave warnings and errors whenever possible instead of silently not reporting unintended behaviors.
            Logger.LogWarning("[DeathUI/OnEnable] - No health found on this gameObject! DeathUI hasn't subscribed to any events when it got enabled!");
            return;
        }
        // Subscribes to the OnDeath event which will trigger when this object's health reaches 0 or below.
        playerHealth.OnDeath += myFunc;
    }

    void myFunc()
    {
        DoThing();
    }
}

class PlayerInventory : Inventory
{
    //same thing as in DeathUI, just subscribe to the OnDeath event.
}
```

_Note: Events can leak! Always remember to unsubscribe your events on `OnDisable()`._

* Minimise Polymorphism, Embrace Interfaces.

[Interface](https://www.w3schools.com/cs/cs_interface.php) in C# is a blueprint of a class. It is like abstract class because all the methods which are declared inside the interface are abstract methods. It cannot have method body and cannot be instantiated. It is used to achieve multiple inheritance which can't be achieved by class.

Interfaces also allow us to easily communicate features between objects and managers while keeping design/implamentation errors contained in predictable states while allowing us to easily expand, maintain and iterate upon them.

_CodeMonkey has a great video on how to utilise interfaces for a modular workflow: [Modular Character System in Unity](https://youtu.be/mJRc9kLxFSk)_

That being said, [Polymorphism](https://www.w3schools.com/cs/cs_polymorphism.php) is not something to be afraid of. Polymorphism makes sense to be used while in the right context, which is mostly going to be in ScriptableObjects and scanerios where you want to work with common data that will be shared around. 

So, use Polymorphism for data driven scenarios while keep systems and functionality bound to C# Interfaces.


* Reduce duplicate code.

If you've seen something being done more than twice in the project, write a common class or class extension that allows us to reduce the likelyhood of having to maintain duplicate code that all do the same thing.

```cs
// bad code.

classA : MonoBehavior{
    void DoX_InTransform();
}

classB : MonoBehavior{
    void DoX_InTransform();
}

classC : MonoBehavior{
    void DoX_InTransform();
}
```

```cs
// good code.
public static class TransformExtensions
{
    //can be called anywhere when they access Transform.
    public static void DoX(this Transform t, float value)
    {
        //Common task here.
    }
}
```

## 2. Performance gotchas!

* Unity still uses Mono, which means we do not get the performance benefits of .NET 6 and 7. As a result, minimise the use of things like [LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/) unless you know what you're doing.
* As a general rule of thumb, avoid doing any performance intensive tasks or calculations on the game server. Your code may only take up 0.5% of CPU time but when 100 systems do this, it becomes inefficent and a glaring issue that someone has to deal with in the future.
* Avoid using the `UpdateManager` whenever possible and bind yourself to an event driven archticture/design for your code. Not only is it good for code quality; But it's also good for performance when your code is designed in a way that expects functions to be called every once and a while rather than every tick/frame.
* `GetComponent<>`, `GetComponentInChildren()` and `TryGetComponent<>()` are largely inefficent and can be incredibly slow. Use `CommonComponents` whenever possible or cache references to components.
```cs
//How to cache references.
[SerializeField] private SpriteHandler itemSprite;

void Awake()
{
    if(itemSprite == null) itemSprite = GetComponentInChildren<SpriteHandler>();
}
```
* [Unity Coroutines](https://docs.unity3d.com/Manual/Coroutines.html) are great for moving tasks that require things to happen over time from the `Update()` loop into an asynchronous workflow that allows you to spread, pause and kill tasks freely over multiple frames. However, do be aware that it's a double edged sword as `IEnumerator` does allocate memory whenever `StartCoroutine` invokes a coroutine and you can only invoke them from inside Monobehaviors. Use them wisely.

_TLDR: Coroutines trades CPU performance for memory_

_"It’s best practice to condense a series of operations down to the fewest number of individual coroutines possible. Nested coroutines are useful for code clarity and maintenance, but they impose a higher memory overhead because the coroutine tracks objects." - Unity Docs._

* Use Time-Splitting whenever your iterators/loops are attempting to go through a large amount of objects/tasks.
```cs
//How to time split.

var currentIndex = 0;
var maximumIndexesPerFrame = 20;

foreach(var item in LargeList)
{
    if(currentIndex >= maximumIndexesPerFrame)
    {
        //Causes the IEnumerator to be paused until the next frame.
        yield return WaitFor.EndOfFrame();
        currentIndex = 0;
    }
    item.DoPossibilyHeavyTask();
    currentIndex += 1;
}
```
* If you're creating a custom data type, use `struct` instead of objects (aka `class`). Structs allocate less memory and are a better choice when your data does not require to be changed overtime or require bundled functions that self manage that data.
* Turn off raycast masks in UI whenever you're not using them! Not only does it prevent bugs where mouse inputs may appeared blocked for no reason; but they also tell Unity to not waste time updating UI elements when there are no mouse interactions made for that element.
* Prioritize using `Vector3.Distance()` rather than raycasts whenever you do not require checking for physical properties in the game world, as functions like `Physics2D.SphereOverlap()` and `MatrixManager.Linecast()` are generally slow and allocate a lot of memory.
* Always use `Stringbuilder` for String concatenation operations as it's much faster than manually editing the string variable directly.
* `TryCatch` has a [very small performance hit](https://stackoverflow.com/questions/1308432/do-try-catch-blocks-hurt-performance-when-exceptions-are-not-thrown) on the game. But it's generally recommended that you avoid using `TryCatch` all together especially on any function that runs on the `UpdateManager` and even more-so if it's on the game server; but if you do need it and have a [good reason](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions) for its use case, always remember to **never** leave your catch methods empty and log your expectations.
```cs
//bad TryCatch code.
try
{
    gameObject.pickupable.GetBestHandOrSlotFor(this);
}
catch
{

}
```
```cs
//good TryCatch code.
try
{
    // Maximise the usecase of trycatch and report extra errors that may or may not happen.
    if(gameObject.pickupable == null)
    {
        throw new ArgumentNullException(paramName: nameof(s), message: "this gameObject has no pickupable components!");
    }
    gameObject.pickupable.GetBestHandOrSlotFor(this);
}
catch (Exception e)
{
    // Never leave your catch block empty, always report that something has went wrong here.
    Logger.LogError($"[ClassName/Function] - An error occured when trying to get the best slot for this item. \n {e}");
}
```