using UnityEngine;
[RequireComponent(typeof(Pickupable))]
public class FoodTrigger : InputTrigger
{

	private FoodBehaviour food;
	void Start()
	{
		food = GetComponent<FoodBehaviour>();
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//TODO: Remove after IF2 refactor
		return false;
	}

	public override void UI_Interact(GameObject originator, string hand)
	{
		food.TryEat();
	}
}
