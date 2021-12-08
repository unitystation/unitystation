using Doors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirlockPainter : MonoBehaviour
{
	[Tooltip("Airlock painting jobs.")]
	public List<GameObject> AvailablePaintJobs;

	public async void ChoosePainJob(GameObject paintableAirlock)
	{
		GameObject chosenPaintJob = await UIManager.RadialMenu.ShowRadialMenu(AvailablePaintJobs, paintableAirlock);
		if(chosenPaintJob != null)
		{
			paintTheAirlock(paintableAirlock, chosenPaintJob);
		}
	}

	private void paintTheAirlock(GameObject paintableAirlock, GameObject chosenPaintJob)
	{
		DoorAnimatorV2 paintJobAnim = chosenPaintJob.GetComponent<DoorAnimatorV2>();
		SpriteHandler paintSprite = paintJobAnim.DoorBase.GetComponent<SpriteHandler>();
		var spriteCatalog = paintSprite.GetSubCatalogue();

		DoorAnimatorV2 airlockAnim = paintableAirlock.GetComponent<DoorAnimatorV2>();
		SpriteHandler airlockSprite = airlockAnim.DoorBase.GetComponent<SpriteHandler>();
		airlockSprite.SetCatalogue(spriteCatalog, 0);
		airlockSprite.SetSpriteSO(spriteCatalog[0]);    //For update the sprite when re-painting
	}
}
