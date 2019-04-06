using UnityEngine;

public class FoodTrigger : PickUpTrigger
{

	private FoodBehaviour food;
	void Start()
	{
		food = GetComponent<FoodBehaviour>();
	}

	public override void UI_Interact(GameObject originator, string hand)
	{
		food.TryEat();
	}
}
