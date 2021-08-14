using Mirror;

public class MetabolismSystemV2 : NetworkBehaviour
{
	private int nutritionLevel = 400;
	public int NutritionLevel => nutritionLevel;

	private HungerState hungerState;
	public HungerState HungerState => hungerState;

	public bool IsHungry => HungerState >= HungerState.Hungry;
	public bool IsStarving => HungerState == HungerState.Starving;


	/// <summary>
	/// Adds a MetabolismEffect to the system. The effect is applied every metabolism tick.
	/// </summary>
	public void AddEffect(MetabolismEffect effect)
	{
		//TODO: Reimplement
	}

}

/// <summary>
/// Used by the Metabolism class to apply the desired effects. Substances are applied evenly over the duration of the effect.
/// </summary>
public struct MetabolismEffect
{



}

public enum HungerState
{
	Full,
	Normal,
	Hungry,
	Malnourished,
	Starving

}