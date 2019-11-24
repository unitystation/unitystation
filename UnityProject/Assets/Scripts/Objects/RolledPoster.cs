
using System;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

public class RolledPoster : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IServerSpawn
{
	public GameObject wallPrefab;
	public PosterBehaviour.Posters posterVariant;
	public SpriteRenderer sprite;
	public Sprite legitSprite;
	public Sprite contrabandSprite;

	private void Awake()
	{

		if (!Globals.IsInitialised)
		{
			JsonImportInitialization();
			Globals.IsInitialised = true;
		}

		sprite = GetComponentInChildren<SpriteRenderer>();
		var attributes = GetComponent<IItemAttributes>();
		var poster = Globals.Posters[posterVariant.ToString()];
		string posterName;
		string desc;
		Sprite icon;

		if (posterVariant == PosterBehaviour.Posters.RandomContraband)
		{
			posterName = "Contraband Poster";
			desc =
				"This poster comes with its own automatic adhesive mechanism, for easy pinning to any vertical surface. Its vulgar themes have marked it as contraband aboard Nanotrasen space facilities.";
			icon = contrabandSprite;
		}
		else if (posterVariant == PosterBehaviour.Posters.RandomOfficial)
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
			icon = poster.Type == PosterBehaviour.PosterType.Contraband ? contrabandSprite : legitSprite;
		}

		attributes.ServerSetItemName(posterName);
		attributes.ServerSetItemDescription(desc);
		sprite.sprite = icon;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		var attributes = GetComponent<IItemAttributes>();
		var poster = Globals.Posters[posterVariant.ToString()];
		string posterName;
		string desc;
		Sprite icon;

		if (posterVariant == PosterBehaviour.Posters.RandomContraband)
		{
			posterName = "Contraband Poster";
			desc =
				"This poster comes with its own automatic adhesive mechanism, for easy pinning to any vertical surface. Its vulgar themes have marked it as contraband aboard Nanotrasen space facilities.";
			icon = contrabandSprite;
		}
		else if (posterVariant == PosterBehaviour.Posters.RandomOfficial)
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
			icon = poster.Type == PosterBehaviour.PosterType.Contraband ? contrabandSprite : legitSprite;
		}

		attributes.ServerSetItemName(posterName);
		attributes.ServerSetItemDescription(desc);
		sprite.sprite = icon;
	}

	private static void JsonImportInitialization()
	{
		var json = (Resources.Load (@"Metadata\Posters") as TextAsset)?.ToString();
		var jsonPosters = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, System.Object>>>(json);
		foreach (KeyValuePair<string, Dictionary<string, System.Object>> entry in jsonPosters)
		{
			var poster = new PosterBehaviour.Poster();
			if (entry.Value.ContainsKey("name"))
			{
				poster.Name = entry.Value["name"].ToString();
			}
			if (entry.Value.ContainsKey("desc"))
			{
				poster.Description = entry.Value["desc"].ToString();
			}
			if (entry.Value.ContainsKey("icon"))
			{
				poster.Icon = entry.Value["icon"].ToString();
			}
			if (entry.Value.ContainsKey("type"))
			{
				poster.Type = (PosterBehaviour.PosterType)int.Parse(entry.Value["type"].ToString());
			}

			Globals.Posters.Add(entry.Key, poster);
		}
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
		wallPrefab.GetComponent<Directional>().InitialDirection = Orientation.From(interaction.Performer.TileWorldPosition() - interaction.WorldPositionTarget).AsEnum();

		Spawn.ServerPrefab(wallPrefab, interaction.WorldPositionTarget.RoundToInt(),
			interaction.Performer.transform.parent);

		Inventory.ServerDespawn(interaction.HandSlot);
	}

	private static class Globals
	{
		public static bool IsInitialised = false;
		public static Dictionary<string, PosterBehaviour.Poster> Posters = new Dictionary<string, PosterBehaviour.Poster>();
	}


}
