using UnityEngine;

public class WeldingFuel : InputTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (originator == PlayerManager.LocalPlayer)
		{
			if (UIManager.Hands.CurrentSlot.Item != null)
			{
				var welder = UIManager.Hands.CurrentSlot.Item.GetComponent<Welder>();
				if (welder != null)
				{
					PlayerManager.PlayerScript.playerNetworkActions.CmdRefillWelder(welder.gameObject, gameObject);
				}
			}
		}

		return true;
	}
}