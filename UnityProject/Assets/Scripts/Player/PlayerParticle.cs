using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class PlayerParticle : NetworkBehaviour
	{
		[SerializeField] private Transform particleHolder;
		private List<PlayerParticleObject> particles = new List<PlayerParticleObject>();

		private void Start()
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

		[ClientRpc]
		public void ToggleParticle(string id, bool state)
		{
			foreach (var particle in particles)
			{
				if (particle.ID != id) continue;
				particle.Reference.SetActive(state);
				return;
			}
			Loggy.LogError($"[PlayerParticle/ToggleParticle] - no such object named {id}.");
		}
	}

	public class PlayerParticleObject
	{
		public GameObject Reference;
		public string ID => Reference.name;
	}
}