using Items;

namespace Items.Dice
{
	public class RollSpaceCube : RollDie
	{
		ItemAttributesV2 itemAttributes;

		protected override void Awake()
		{
			base.Awake();
			itemAttributes = GetComponent<ItemAttributesV2>();
		}

		protected override void Start()
		{
			base.Start();

			if (DMMath.Prob(10))
			{
				itemAttributes.ServerSetArticleName("spess cube");
			}
		}
	}
}
