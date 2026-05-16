using UnityEngine;
using US13.Player;

public class SetColourEffect : MonoBehaviour, IWantMoreEffectInfo
{
	[SerializeField] private bool UseIWantMoreEffectInfo;

	public Color ColourToSet =  Color.white;

	public bool ShowAllBrightToOwner = false;

	private void OnEnable()
	{
		if (UseIWantMoreEffectInfo == false)
		{
			SetColour(ColourToSet);
		}

	}

	public void SetColour(Color Colour)
	{
		var Sprites = GetComponentsInChildren<SpriteRenderer>();
		foreach (var Sprite in Sprites)
		{
			Sprite.color = Colour;
		}
	}

	public void SetBrightTo()
	{
		var Sprites = GetComponentsInChildren<SpriteRenderer>();
		foreach (var Sprite in Sprites)
		{
			Sprite.gameObject.layer = 5; //5 == UI
		}
	}

	public void ReceiveMoreEffectInfo(Color Colour, GameObject Owner)
	{
		if (UseIWantMoreEffectInfo) SetColour(Colour);
		if (ShowAllBrightToOwner)
		{
			if (Owner == PlayerManager.LocalPlayerScript.gameObject)
			{
				SetBrightTo();
			}
		}


	}
}
