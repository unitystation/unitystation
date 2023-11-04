using System.Collections;
using System.Collections.Generic;
using Logs;
using Mirror;
using Newtonsoft.Json;
using Player;
using UI.CharacterCreator;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart : MonoBehaviour
	{
		/// <summary>
		/// What is this BodyPart's sprite's tone if it shared a skin tone with the player?
		/// </summary>
		[HideInInspector] public Color? Tone;

		/// <summary>
		/// The prefab sprites for this body part
		/// </summary>
		[Tooltip("The prefab sprites for this")]
		public BodyPartSprites SpritePrefab;

		[Tooltip("The body part's pickable item's sprites.")]
		public SpriteHandler BodyPartItemSprite;

		[Tooltip(
			"Does this body part share the same color as the player's skintone when it deattatches from his body?")]
		public bool BodyPartItemInheritsSkinColor = false;


		/// <summary>
		/// Custom settings from the lobby character designer
		/// </summary>
		[Tooltip("Custom options from the Character Customizer that modifys this")]
		public BodyPartCustomisationBase LobbyCustomisation;

		[Tooltip("List of optional body added to this, eg what wings a Moth has")] [SerializeField]
		private List<BodyPart> optionalOrgans = new List<BodyPart>();

		/// <summary>
		/// The list of optional body that are attached/stored in this body part, eg what wings a Moth has
		/// </summary>
		public List<BodyPart> OptionalOrgans => optionalOrgans;

		/// <summary>
		/// The list of optional body that can be attached/stored in this body part, eg what wings are available on a Moth chest
		/// </summary>
		[Tooltip("List of body parts this can be replaced with")]
		public List<BodyPart> OptionalReplacementOrgan = new List<BodyPart>();

		/// <summary>
		/// Flag that is true if the body part is external (exposed to the outside world), false if it is internal
		/// </summary>
		[Tooltip("Is the body part on the surface?")]
		public bool IsSurface = false;

		private void RemoveAllSprites()
		{
			for (var i = RelatedPresentSprites.Count - 1; i >= 0; i--)
			{
				RemoveSprite(RelatedPresentSprites[i], false);
			}

			HealthMaster.rootBodyPartController.UpdateClients();
		}

		/// <summary>
		/// Sets up the sprite of a specified body part and adds its Net ID to InternalNetIDs
		/// </summary>
		public void ServerCreateSprite()
		{
			BodyType bodyType = BodyType.NonBinary;
			if (playerSprites.ThisCharacter != null)
			{
				bodyType = playerSprites.ThisCharacter.BodyType;
			}

			var sprites = this.GetBodyTypeSprites(bodyType); //TODO maybe make Sprite generation as part of customisation OnPlayerBodyDeserialise Idk
			foreach (var Sprite in sprites.Item2)
			{
				RegisterNewSprite(SpritePrefab.gameObject, sprites.Item1, Sprite, false);
			}

			HealthMaster.rootBodyPartController.UpdateClients();

			if (SetCustomisationData != "")
			{
				LobbyCustomisation.OnPlayerBodyDeserialise(this, SetCustomisationData, HealthMaster);
			}
		}


		public void RemoveSprite(BodyPartSprites ToRemove, bool UpdateMaster = true)
		{
			if (RelatedPresentSprites.Contains(ToRemove))
			{
				if (IsSurface || BodyPartItemInheritsSkinColor)
				{
					playerSprites.SurfaceSprite.Remove(ToRemove);
				}

				RelatedPresentSprites.Remove(ToRemove);
				playerSprites.Addedbodypart.Remove(ToRemove);
				SpriteHandlerManager.UnRegisterHandler(playerSprites.GetComponent<NetworkIdentity>(),
					ToRemove.baseSpriteHandler);
				HealthMaster.InternalNetIDs.Remove(ToRemove.intName);
				Destroy(ToRemove.gameObject);
			}

			if (UpdateMaster)
			{
				HealthMaster.rootBodyPartController.UpdateClients();
			}
		}

		public BodyPartSprites RegisterNewSprite(GameObject SpritePrefab, SpriteOrder SpriteOrder,  SpriteDataSO sprite, bool UpdateMaster = true)
		{
			bool isSurfaceSprite = IsSurface || BodyPartItemInheritsSkinColor;

			var newSprite = Spawn.ServerPrefab(SpritePrefab, Vector3.zero, playerSprites.BodySprites.transform).GameObject.GetComponent<BodyPartSprites>();

			newSprite.transform.localPosition = Vector3.zero;
			playerSprites.Addedbodypart.Add(newSprite);

			var newOrder = new SpriteOrder(SpriteOrder);
			newOrder.Add(this.RelatedPresentSprites.Count * 3); // ???????????????????????? for Sprite order clashes, for example hands not rendering over jumpsuit

			this.RelatedPresentSprites.Add(newSprite);

			var ClientData = new IntName();

			ClientData.Name = name + "_" + newSprite.GetInstanceID() ; //is Fine because name is being Networked
			newSprite.SetName(ClientData.Name);
			ClientData.Int = CustomNetworkManager.Instance.IndexLookupSpawnablePrefabs[SpritePrefab.gameObject];
			ClientData.Data = JsonConvert.SerializeObject(newOrder);

			newSprite.intName = ClientData;

			HealthMaster.InternalNetIDs.Add(ClientData);

			newSprite.baseSpriteHandler.NetworkThis = true;

			SpriteHandlerManager.RegisterHandler(playerSprites.GetComponent<NetworkIdentity>(), newSprite.baseSpriteHandler);

			newSprite.UpdateSpritesForImplant(this, this.ClothingHide, sprite, newOrder);
			if (isSurfaceSprite)
			{
				playerSprites.SurfaceSprite.Add(newSprite);
				HandleSurface(newSprite);
			}

			if (UpdateMaster)
			{
				HealthMaster.rootBodyPartController.UpdateClients();
			}

			return newSprite;
		}

		public void HandleSurface(BodyPartSprites newSprite)
		{
			Color CurrentSurfaceColour = Color.white;
			if (this.Tone == null) //Has no tone set
			{
				if (playerSprites.RaceBodyparts.Base.SkinColours.Count > 0)
				{
					if (playerSprites.ThisCharacter == null)
					{
						Loggy.LogError("playerSprites.ThisCharacter == null");
						return;
					}

					ColorUtility.TryParseHtmlString(playerSprites.ThisCharacter.SkinTone, out CurrentSurfaceColour);

					var hasColour = false;

					foreach (var color in playerSprites.RaceBodyparts.Base.SkinColours)
					{
						if (color.ColorApprox(CurrentSurfaceColour))
						{
							hasColour = true;
							break;
						}
					}

					if (hasColour == false)
					{
						CurrentSurfaceColour = playerSprites.RaceBodyparts.Base.SkinColours[0];
					}
				}
				else
				{
					ColorUtility.TryParseHtmlString(playerSprites.ThisCharacter.SkinTone, out CurrentSurfaceColour);
				}
			}
			else //Already has tone set
			{
				CurrentSurfaceColour = this.Tone.Value;
			}

			CurrentSurfaceColour.a = 1;
			newSprite.baseSpriteHandler.SetColor(CurrentSurfaceColour);
			Tone = CurrentSurfaceColour;
			BodyPartItemSprite.SetColor(CurrentSurfaceColour);


		}

		public void SetCustomisationString(string data)
		{
			SetCustomisationData = data;
			if (SetCustomisationData != "")
			{
				LobbyCustomisation.OnPlayerBodyDeserialise(this, SetCustomisationData, HealthMaster);
			}
		}
	}
}