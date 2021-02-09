using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blob;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class UI_Blob : MonoBehaviour
{
	//UI buttons
	public TMP_Text healthText = null;
	public TMP_Text resourceText = null;
	public TMP_Text numOfBlobTilesText = null;
	public TMP_Text strainRerollsText = null;

	//Strain screen
	[SerializeField]
	private GameObject strainScreen = null;
	public TMP_Text strainNameText = null;
	public TMP_Text strainDescText = null;
	public TMP_Text strainPlayerDamageText = null;
	public TMP_Text strainObjectDamageText = null;
	public TMP_Text strainArmorText = null;

	public int? clickedStrainIndex = null;

	public List<BlobStrain> randomStrains = new List<BlobStrain>();

	[SerializeField]
	private List<StrainButton> randomStrainsButtons = new List<StrainButton>();

	[SerializeField]
	private GameObject overlayNode = null;
	[SerializeField]
	private GameObject overlayStrong = null;
	[SerializeField]
	private GameObject overlayReflective = null;
	[SerializeField]
	private GameObject overlayFactory = null;
	[SerializeField]
	private GameObject overlayResource = null;
	[SerializeField]
	private GameObject overlayRemoveBlob = null;
	[SerializeField]
	private GameObject overlayRally = null;
	[SerializeField]
	private GameObject overlayMoveCore = null;

	private bool node;
	private bool strong;
	private bool reflective;
	private bool factory;
	private bool resource;
	private bool remove;
	private bool rally;
	private bool core;

	[HideInInspector]
	public bool blobnet;

	[HideInInspector]
	public BlobPlayer blobPlayer = null;

	[HideInInspector]
	public BlobMouseInputController controller = null;

	public void ToggleBlobNet()
	{
		if (blobPlayer == null) return;

		blobnet = !blobnet;

		blobPlayer.ToggleLineRenderers(blobnet);
	}
	public void JumpToCore()
	{
		if (blobPlayer == null) return;

		blobPlayer.CmdTeleportToCore();
	}

	public void JumpToNode()
	{
		if (blobPlayer == null) return;

		blobPlayer.CmdTeleportToNode();
	}

	/// <summary>
	/// Alternative to alt click
	/// </summary>
	public void RemoveBlob()
	{
		if (remove)
		{
			overlayRemoveBlob.SetActive(false);
			controller.placeOther = false;
			blobPlayer.CmdToggleRemove(true);
			remove = false;
			return;
		}

		ClearBools();
		remove = true;

		controller.placeOther = false;
		blobPlayer.CmdToggleRemove(false);
		ClearOutline();
		overlayRemoveBlob.SetActive(true);
	}

	public void RallySpores()
	{
		Chat.AddExamineMsgToClient("The blob has yet to evolve to command these.");

		return;

		if (rally)
		{
			overlayRally.SetActive(false);
			controller.placeOther = false;
			rally = false;
			return;
		}

		ClearBools();
		rally = true;

		ClearOutline();
		overlayRally.SetActive(controller.placeOther);
	}

	public void RelocateCore()
	{
		if (core)
		{
			overlayMoveCore.SetActive(false);
			controller.placeOther = false;
			core = false;
			return;
		}

		ClearBools();
		core = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Core;
		ClearOutline();
		overlayMoveCore.SetActive(controller.placeOther);
	}

	public void PlaceNode()
	{
		if (node)
		{
			overlayNode.SetActive(false);
			controller.placeOther = false;
			node = false;
			return;
		}

		ClearBools();
		node = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Node;
		ClearOutline();
		overlayNode.SetActive(controller.placeOther);
	}

	public void PlaceStrong()
	{
		if (strong)
		{
			overlayStrong.SetActive(false);
			controller.placeOther = false;
			strong = false;
			return;
		}

		ClearBools();
		strong = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Strong;
		ClearOutline();
		overlayStrong.SetActive(controller.placeOther);
	}

	public void PlaceReflective()
	{
		if (reflective)
		{
			overlayReflective.SetActive(false);
			controller.placeOther = false;
			reflective = false;
			return;
		}

		ClearBools();
		reflective = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Reflective;
		ClearOutline();
		overlayReflective.SetActive(controller.placeOther);
	}

	public void PlaceFactory()
	{
		if (factory)
		{
			overlayFactory.SetActive(false);
			controller.placeOther = false;
			factory = false;
			return;
		}

		ClearBools();
		factory = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Factory;
		ClearOutline();
		overlayFactory.SetActive(controller.placeOther);
	}

	public void PlaceResource()
	{
		if (resource)
		{
			overlayResource.SetActive(false);
			controller.placeOther = false;
			resource = false;
			return;
		}

		ClearBools();
		resource = true;

		controller.placeOther = !controller.placeOther;
		controller.blobConstructs = BlobConstructs.Resource;
		ClearOutline();
		overlayResource.SetActive(controller.placeOther);
	}

	public void ClearOutline()
	{
		overlayFactory.SetActive(false);
		overlayNode.SetActive(false);
		overlayReflective.SetActive(false);
		overlayResource.SetActive(false);
		overlayStrong.SetActive(false);
		overlayMoveCore.SetActive(false);
		overlayRally.SetActive(false);
		overlayRemoveBlob.SetActive(false);
	}

	public void ClearBools()
	{
		node= false;
		strong= false;
		reflective= false;
		factory= false;
		resource= false;
		remove= false;
		rally= false;
		core= false;
	}

	#region Strains

	public void ReadaptStrain()
	{
		if (clickedStrainIndex == null)
		{
			Chat.AddExamineMsgToClient("Select a strain to readapt into first");
			return;
		}

		blobPlayer.CmdChangeStrain(clickedStrainIndex.Value);

		clickedStrainIndex = null;
	}

	public void GetRandomStrains()
	{
		blobPlayer.CmdRandomiseStrains();
	}

	public void SetActiveButton(int buttonClicked)
	{
		var strainIndex = randomStrainsButtons[buttonClicked].strainIndex;

		foreach (var button in randomStrainsButtons)
		{
			button.selectedImage.color = Color.white;
		}

		randomStrainsButtons[buttonClicked].selectedImage.color = Color.green;
		clickedStrainIndex = strainIndex;
		GenerateBlobStrainData(blobPlayer.blobStrains[strainIndex]);
	}

	public void OpenCloseStrainScreen()
	{
		strainScreen.SetActive(!strainScreen.activeSelf);

		if (strainScreen.activeSelf)
		{
			UpdateStrainInfo();
		}
	}

	public void UpdateStrainInfo()
	{
		GenerateBlobStrainData(blobPlayer.clientCurrentStrain);
		ResetButtons();
	}

	private void GenerateBlobStrainData(BlobStrain newblobStrain)
	{
		var playerDamage = new StringBuilder();
		var objectDamage = new StringBuilder();
		var armor = new StringBuilder();

		foreach (var damage in newblobStrain.objectDamages)
		{
			objectDamage.Append($"{damage.damageType}: {damage.damageDone}\n");
		}

		foreach (var damage in newblobStrain.playerDamages)
		{
			playerDamage.Append($"{damage.damageType}: {damage.damageDone}\n");
		}

		var attackTypes = Enum.GetValues(typeof(AttackType));

		foreach (AttackType attackType in attackTypes)
		{
			var rating = newblobStrain.armor.GetRating(attackType);
			var colour = "<color=green>";

			if (rating < 0)
			{
				colour = "<color=red>";
			}

			armor.Append($"{attackType}: {colour}{rating}%</color>\n");
		}

		strainNameText.text = newblobStrain.strainName;
		strainDescText.text = newblobStrain.strainDesc;
		strainPlayerDamageText.text = "Player Damage: \n\n" + playerDamage;
		strainObjectDamageText.text = "Object Damage: \n\n" + objectDamage;
		strainArmorText.text = "Armor: \n\n" + armor;
	}

	private void ResetButtons()
	{
		//First time generating
		if (!randomStrains.Any())
		{
			var strains = blobPlayer.blobStrains.Where(s => s != blobPlayer.clientCurrentStrain);
			randomStrains = strains.PickRandom(4).ToList();
		}

		for (var i = 0; i < 4; i++)
		{
			randomStrainsButtons[i].buttonText.text = randomStrains[i].strainName;
			randomStrainsButtons[i].strainIndex = blobPlayer.blobStrains.IndexOf(randomStrains[i]);
			randomStrainsButtons[i].selectedImage.color = Color.white;
		}

		clickedStrainIndex = null;
	}

	#endregion

	[Serializable]
	public class StrainButton
	{
		public TMP_Text buttonText;
		public Image selectedImage;
		public int strainIndex;
	}
}
