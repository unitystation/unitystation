using Items;

public class BaseFood : PickUpTrigger
{
    /// <summary>
    /// EatFood is called from Hands.cs Use() function. To make an Item food, change the SpriteType to food,
    /// and attach a script that inherits from this script, BaseFood.cs. Make a public override void EatFood()
    /// method in your new script, and place any code you want to run when the player eats food in there. Check
    /// out the MeatSteak.cs script for a working detail.
    /// </summary>
    public virtual void EatFood()
    {

    }
}
