using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PosterBehaviour : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public SpriteRenderer sprite;
	public GameObject rolledPosterPrefab;

	[SyncVar (hook = nameof(SyncPosterType))]
	public Posters posterVariant = Posters.Random;

	public List<Poster> OfficialPosters = new List<Poster>();
	public List<Poster> ContrabandPosters = new List<Poster>();
	public List<Poster> OtherPosters = new List<Poster>();

	public override void OnStartClient()
	{
		var starterPoster = GetPoster(posterVariant);
		posterVariant = starterPoster.PosterName;
		SyncPosterType(posterVariant, posterVariant);
		base.OnStartClient();
	}

	private void SyncPosterType(Posters oldP, Posters p)
	{
		posterVariant = p;

		var poster = GetPoster(p);
		if (poster != null)
		{
			sprite.sprite = poster.sprite;
		}
	}

	public Poster GetPoster(Posters p)
	{
		if (p == Posters.Ripped)
		{
			return OtherPosters[0];
		}

		if (p == Posters.Random)
		{
			if (Random.value < 0.5f)
			{
				return OfficialPosters[Random.Range(0, OfficialPosters.Count - 1)];
			}
			else
			{
				return ContrabandPosters[Random.Range(0, OfficialPosters.Count - 1)];
			}
		}

		if (p == Posters.RandomOfficial)
		{
			return OfficialPosters[Random.Range(0, OfficialPosters.Count - 1)];
		}

		if (p == Posters.RandomContraband)
		{
			return ContrabandPosters[Random.Range(0, OfficialPosters.Count - 1)];
		}

		var index = OfficialPosters.FindIndex(x => x.PosterName == p);
		if (index != -1)
		{
			return OfficialPosters[index];
		}
		else
		{
			index = ContrabandPosters.FindIndex(x => x.PosterName == p);
			if (index != -1)
			{
				return ContrabandPosters[index];
			}
		}

		return null;
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
			SoundManager.PlayNetworkedAtPos("WireCutter", pos, 1f, sourceObj: gameObject);

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
		                       " rips the poster in a single, decisive motion!", pos, gameObject);
		SoundManager.PlayNetworkedAtPos("PosterRipped", pos, sourceObj: gameObject);

		SyncPosterType(posterVariant, Posters.Ripped);
	}
}
