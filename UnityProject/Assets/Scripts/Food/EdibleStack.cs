/// <summary>
///     Edible stack function for foods that stack. Multiplies satiation and heal amount by number in stack before eating.
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
			//Multiply hunger and heal by the amoount of items stored, then try eat.
			healAmount = stckCmp.Amount*healAmount;
			healHungerAmount = stckCmp.Amount*healHungerAmount;
			base.TryEat();
		}
		
	}
}