/// <summary>
///     Edible stack function for foods that stack. Reduces number on eat instead of eating the whole item.
/// </summary>
public class EdibleStack : Edible
{
	//Stacking component for the object.
	private Stackable stckCmp;
	private void Awake()
	{
		stckCmp = gameObject.GetComponent<Stackable>();

	}

	//TODO Implement this. Turns out eating this would require refactoring player eating method which
	//I'm too scared to do yet. Right now it consumes the whole stack, but multiplies initial hunger/heal
	//data by the amount stored.
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