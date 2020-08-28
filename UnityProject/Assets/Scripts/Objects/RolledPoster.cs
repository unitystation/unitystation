using UnityEngine;
using Mirror;

public class RolledPoster : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject wallPrefab;
	[SyncVar (hook = nameof(SyncPosterType))]
	public Posters posterVariant;
	public SpriteRenderer spriteRend;
	public Sprite legitSprite;
	public Sprite contrabandSprite;

	public override void OnStartServer()
	{
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
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		var interactableTiles = interaction.TargetObject.GetComponent<InteractableTiles>();
		if (!interactableTiles)
		{
			return false;
		}

		if (!MatrixManager.IsWallAt(interaction.WorldPositionTarget.RoundToInt(), side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		wallPrefab.GetComponent<PosterBehaviour>().posterVariant = posterVariant;
		wallPrefab.GetComponent<Directional>().InitialDirection = Orientation
			.From(interaction.Performer.TileWorldPosition() - interaction.WorldPositionTarget).AsEnum();

		Spawn.ServerPrefab(wallPrefab, interaction.WorldPositionTarget.RoundToInt(),
			interaction.Performer.transform.parent);

		Inventory.ServerDespawn(interaction.HandSlot);
	}
}
