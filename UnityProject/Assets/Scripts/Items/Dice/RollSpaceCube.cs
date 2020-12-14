using Items;

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

		if (GetProbability(10))
		{
			itemAttributes.ServerSetArticleName("spess cube");
		}
	}
}
