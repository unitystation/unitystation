
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

public class PosterBehaviour : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteRenderer sprite;
	public GameObject rolledPosterPrefab;

	[SyncVar (hook = nameof(SyncPosterType))]
	public Posters posterVariant = Posters.Random;

	public List<Poster> OfficialPosters = new List<Poster>();
	public List<Poster> ContrabandPosters = new List<Poster>();
	public List<Poster> OtherPosters = new List<Poster>();

	private void Awake()
	{
		if (!Globals.IsInitialised)
		{
			JsonImportInitialization();
			Globals.IsInitialised = true;
		}
	}

	public override void OnStartClient()
	{
		SyncPosterType(this.posterVariant);
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		SyncPosterType(this.posterVariant);
		base.OnStartServer();
	}

	private void SyncPosterType(Posters p)
	{
		posterVariant = p;

		var posters = new List<string>();
		if (posterVariant == Posters.RandomOfficial
		    || posterVariant == Posters.Random)
		{
			posters.AddRange(Globals.OfficialPosters.Keys.ToList());
		}
		if (posterVariant == Posters.RandomContraband
		    || posterVariant == Posters.Random)
		{
			posters.AddRange(Globals.ContrabandPosters.Keys.ToList());
		}

		if (posters.Count > 0)
		{
			Enum.TryParse(posters[UnityEngine.Random.Range(0, posters.Count)], out Posters variant);
			posterVariant = variant;
		}

		Poster poster = null;
		var posterName = posterVariant.ToString();
		if (Globals.OfficialPosters.ContainsKey(posterName))
		{
			poster = Globals.OfficialPosters[posterName];
		}
		else if (Globals.ContrabandPosters.ContainsKey(posterName))
		{
			poster = Globals.ContrabandPosters[posterName];
		}
		else if (Globals.OtherPosters.ContainsKey(posterName))
		{
			poster = Globals.OtherPosters[posterName];
		}

		if (poster != null)
		{
			sprite.sprite = Resources.Load<Sprite>("textures/objects/contraband/contraband_" + poster.Icon);
		}
	}

	[ContextMenu("Load all posters")]
	private void JsonImportInitialization()
	{
		var json = (Resources.Load (@"Metadata\Posters") as TextAsset)?.ToString();
		var jsonPosters = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, System.Object>>>(json);
		foreach (KeyValuePair<string, Dictionary<string, System.Object>> entry in jsonPosters)
		{
			var poster = new Poster();
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
				poster.Type = (PosterType)int.Parse(entry.Value["type"].ToString());
			}

			poster.sprite = PosterSpriteLoader(poster.Icon);

			switch (poster.Type)
			{
				case PosterType.None:
					OtherPosters.Add(poster);
					break;
				case PosterType.Official:
					OfficialPosters.Add(poster);
					break;
				case PosterType.Contraband:
					ContrabandPosters.Add(poster);
					break;
			}
		}
	}

	Sprite PosterSpriteLoader(string icon)
	{
		return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/objects/contraband/contraband_{icon}.png");
	}

	// Only interact with empty hands and wirecutter
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (interaction.TargetObject != gameObject) return false;

		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();
		var item = pna.GetActiveHandItem();
		if (Validations.HasItemTrait(item, CommonTraits.Instance.Wirecutter))
		{
			return true;
		}

		return item == null && posterVariant != Posters.Ripped;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var pos = interaction.Performer.WorldPosServer();
		var pna = interaction.Performer.GetComponent<PlayerNetworkActions>();
		var item = pna.GetActiveHandItem();
		if (Validations.HasItemTrait(item, CommonTraits.Instance.Wirecutter))
		{
			SoundManager.PlayNetworkedAtPos("WireCutter", pos, 1f);

			if (posterVariant == Posters.Ripped)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You carefully remove the remnants of the poster.");
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You carefully remove the poster from the wall.");

				rolledPosterPrefab.GetComponent<RolledPoster>().posterVariant = posterVariant;

				Spawn.ServerPrefab(rolledPosterPrefab, pos, interaction.Performer.transform.parent);
			}

			Despawn.ServerSingle(gameObject);

			return;
		}

		if (posterVariant == Posters.Ripped)
		{
			return;
		}

		Chat.AddLocalMsgToChat(interaction.Performer.ExpensiveName() +
		                       " rips the poster in a single, decisive motion!", pos);
		SoundManager.PlayNetworkedAtPos("PosterRipped", pos);

		SyncPosterType(Posters.Ripped);
	}

	private static class Globals
	{
		public static bool IsInitialised = false;
		public static Dictionary<string, Poster> OfficialPosters = new Dictionary<string, Poster>();
		public static Dictionary<string, Poster> ContrabandPosters = new Dictionary<string, Poster>();
		public static Dictionary<string, Poster> OtherPosters = new Dictionary<string, Poster>();
	}
}


