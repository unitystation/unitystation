/// <summary>
///     Edible stack function for foods that stack. Multiplies satiation by number in stack before eating.
/// </summary>
public class EdibleStack : Edible
{
	//Stacking component for the object.
	private Stackable stckCmp;
	private void Awake()
	{
		stckCmp = gameObject.GetComponent<Stackable>();

	}

	public override void TryEat()
	{
		//Check if stack component exists. If not, consume whole stack normally.
		if (stckCmp == null)
		{
			base.TryEat();

		} else {
			//Multiply nutrient gain by the amoount of items stored, then try eat.
			NutrientsHealAmount = stckCmp.Amount*NutrientsHealAmount;
			base.TryEat();
		}
		
	}
}