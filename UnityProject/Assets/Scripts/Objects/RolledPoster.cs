using System;
using UnityEngine;
using Mirror;
using Objects;
using SecureStuff;
using UnityEngine.Serialization;

namespace Items
{
	public class RolledPoster : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		public GameObject wallPrefab;

	    [FormerlySerializedAs("posterVariant")]	public Posters InitialPoster;
		[SyncVar(hook = nameof(SyncPosterType))]
		[PlayModeOnly, NonSerialized] public Posters posterVariant;
		public SpriteRenderer spriteRend;
		public Sprite legitSprite;
		public Sprite contrabandSprite;

		public override void OnStartServer()
		{
			posterVariant = InitialPoster;
			var startPoster = wallPrefab.GetComponent<PosterBehaviour>().GetPoster(posterVariant);
			posterVariant = startPoster.PosterName;
			SyncPosterType(posterVariant, posterVariant);
			base.OnStartServer();
		}

		public void SyncPosterType(Posters oldP, Posters p)
		{
			posterVariant = p;

			var attributes = GetComponent<ItemAttributesV2>();
			var poster = wallPrefab.GetComponent<PosterBehaviour>().GetPoster(p);
			string posterName;
			string desc;
			Sprite icon;

			if (posterVariant == Posters.RandomContraband)
			{
				posterName = "Contraband Poster";
				desc =
					"This poster comes with its own automatic adhesive mechanism, for easy pinning to any vertical surface. Its vulgar themes have marked it as contraband aboard Nanotrasen space facilities.";
				icon = contrabandSprite;
			}
			else if (posterVariant == Posters.RandomOfficial)
			{
				posterName = "Motivational Poster";
				desc =
					"An official Nanotrasen-issued poster to foster a compliant and obedient workforce. It comes with state-of-the-art adhesive backing, for easy pinning to any vertical surface.";
				icon = legitSprite;
			}
			else
			{
				posterName = poster.Name;
				desc = poster.Description;
				icon = poster.Type == PosterType.Contraband ? contrabandSprite : legitSprite;
			}

			attributes.ServerSetArticleName(posterName);
			attributes.ServerSetArticleDescription(desc);
			spriteRend.sprite = icon;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false)
			{
				return false;
			}

			var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
			if (interactableTiles == false)
			{
				return false;
			}

			if (MatrixManager.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), side == NetworkSide.Server) == false)
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var wall = Spawn.ServerPrefab(wallPrefab, interaction.WorldPositionTarget.RoundToInt(),
				interaction.Performer.transform.parent).GameObject;

			wall.GetComponent<PosterBehaviour>().posterVariant = posterVariant;
			wall.GetComponent<Rotatable>().SetFaceDirectionLocalVector((interaction.Performer.TileLocalPosition() - interaction.TargetPosition).RoundTo2Int());

			Inventory.ServerDespawn(interaction.HandSlot);
		}
	}
}
