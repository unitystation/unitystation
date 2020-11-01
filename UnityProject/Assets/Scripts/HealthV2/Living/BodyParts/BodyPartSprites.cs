using HealthV2;
using UnityEngine;

/// <summary>
/// This is used to contain data about the basic rendering of a body part.
/// It will have info about what to render when there's no limb, the position of the rendering, etc.
/// </summary>
public class BodyPartSprites : MonoBehaviour
{
	[SerializeField]
	private SpriteHandler baseSpriteHandler;

	[SerializeField]
	private SpriteHandler baseOverlaySpriteHandler;

	[SerializeField]
	private SpriteHandler damageOverlaySpriteHandler;

	[SerializeField]
	[Tooltip("The sprites that will be used when there is no limbs installed.")]
	private SpriteDataSO noLimbSpriteData;

	public CharacterSettings ThisCharacter;


	public void UpdateSpritesForImplant(ImplantBase implant)
	{
		if (implant.LimbSpriteData)
		{
			baseSpriteHandler.SetSpriteSO(implant.LimbSpriteData);
		}

		if (implant.LimbOverlaySpriteData)
		{
			baseOverlaySpriteHandler.SetSpriteSO(implant.LimbOverlaySpriteData);
		}

	}

	public void UpdateSpritesOnImplantRemoved(ImplantBase implant)
	{

	}
}
