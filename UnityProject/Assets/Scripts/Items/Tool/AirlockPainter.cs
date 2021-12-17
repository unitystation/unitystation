using Doors;
using Objects.Construction;
using System.Collections.Generic;
using UnityEngine;

public class AirlockPainter : MonoBehaviour, IClientInteractable<HandActivate>
{
	[Tooltip("Airlock painting jobs.")]
	public List<GameObject> AvailablePaintJobs;

	private int currentPaintJobIndex = -1;

	public int CurrentPaintJobIndex
	{
		get => currentPaintJobIndex;
		set => currentPaintJobIndex = value;
	}

	public async void ChoosePainJob(GameObject performer)
	{
		if (AvailablePaintJobs == null) return;

		GameObject chosenPaintJob = await UIManager.RadialMenu.ShowRadialMenu(AvailablePaintJobs, performer);
		if (chosenPaintJob)
		{
			int chosenPaintJobIndex = AvailablePaintJobs.IndexOf(chosenPaintJob);
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetPaintJob(chosenPaintJobIndex);
		}
	}

	public void ServerPaintTheAirlock(GameObject paintableAirlock)
	{
		if(currentPaintJobIndex == -1)
		{
			return;
		}
		GameObject airlockWindowed = AvailablePaintJobs[currentPaintJobIndex];
		DoorAnimatorV2 paintJobAnim = airlockWindowed.GetComponent<DoorAnimatorV2>();
		SpriteHandler paintSprite = paintJobAnim.DoorBase.GetComponent<SpriteHandler>();
		var spriteCatalog = paintSprite.GetSubCatalogue();

		DoorAnimatorV2 airlockAnim = paintableAirlock.GetComponent<DoorAnimatorV2>();
		SpriteHandler airlockSprite = airlockAnim.DoorBase.GetComponent<SpriteHandler>();
		airlockSprite.SetCatalogue(spriteCatalog, 0);
		airlockSprite.SetSpriteSO(spriteCatalog[0]);    //For update the sprite when re-painting
	}

	public bool Interact(HandActivate interaction)
	{
		ChoosePainJob(interaction.Performer);
		return true;
	}

}

