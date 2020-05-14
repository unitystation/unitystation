# Development Standards Guide

Having a consistent code style and basic standards for code quality helps to keep our code "clean", making it easier to develop as our project grows. It would be awesome to build a game as complex as SS13 that is also easy to keep improving upon. Remember that each line of code you add now will be read by many other fellow developers in the future (including you!), so writing code that's easy to understand has a very positive impact on the project. 

These are not strict rules, and reviewers don't expect developers to be able to follow all of these suggestions, but we strive to adhere to them as much as possible. When a reviewer looks at your PR, part of their review will involve thinking about code cleanliness. They will work with you to figure out if / what changes can be made to the PR to help with code cleanliness. They might suggest some items from this list or something from their own experience. As you submit more and more PRs, the reviewers you collaborate with should help you develop a better sense of how to write clean code until it becomes second nature.

As a supplement to this, you can check out some best practices for Unity development here: [50 Tips and Best Practices for Unity](https://www.gamasutra.com/blogs/HermanTulleken/20160812/279100/50_Tips_and_Best_Practices_for_Unity_2016_Edition.php)

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
        public bool Interact(GameObject originator, string hand) {

2. Usually, use a separate .cs file for each public class or struct. It is sometimes preferable to have more than one in the same file, but if uncertain err on the side of using a separate file.

3. Use PascalCase for all file names. 

3. Use namespace declarations matching the script's folder path on all new or modified script files.

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
        
