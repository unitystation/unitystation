using US13.Items.Traits;

namespace US13.Items.Food
{
	public class Drink : Edible
	{
		private void Start()
		{
			//assuming all drinks are spillable on throw
			if (itemAttributes)
			{
				itemAttributes.AddTrait(CommonTraits.Instance.SpillOnThrow);
			}
		}
	}
}
