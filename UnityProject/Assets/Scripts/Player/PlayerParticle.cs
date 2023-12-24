using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class PlayerParticle : NetworkBehaviour
	{
		[SerializeField] private Transform particleHolder;
		[SyncVar(hook = nameof(OnActiveIdListChange))] private List<string> activeIDs = new List<string>();
		private List<PlayerParticleObject> particles = new List<PlayerParticleObject>();

		private void Awake()
		{
			for (int i = 0; i < particleHolder.childCount - 1; i++)
			{
				var obj = particleHolder.GetChild(i).gameObject;
				PlayerParticleObject particleObject = new PlayerParticleObject()
				{
					Reference = obj,
				};
				particles.Add(particleObject);
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			OnActiveIdListChange(activeIDs, activeIDs);
		}

		[Server]
		public void ServerToggleParticle(string id, bool state)
		{
			foreach (var particle in particles)
			{
				if (particle.ID != id) continue;
				particle.Reference.SetActive(state);
				if (state == false)
				{
					activeIDs.Remove(id);
				}
				else
				{
					activeIDs.Add(id);
				}
				return;
			}
			Loggy.LogError($"[PlayerParticle/ToggleParticle] - no such object named {id}.");
		}

		public void OnActiveIdListChange(List<string> oldState, List<string> newState)
		{
			if (particles == null || particles.Count == 0) return;
			foreach (var particle in particles)
			{
				particle.Reference.SetActive(newState.Contains(particle.ID));
			}
		}
	}

	public class PlayerParticleObject
	{
		public GameObject Reference;
		public string ID => Reference.name;
	}
}