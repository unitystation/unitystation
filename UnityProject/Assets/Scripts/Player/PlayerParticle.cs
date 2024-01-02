using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class PlayerParticle : NetworkBehaviour
	{
		[SerializeField] private Transform particleHolder;
		private SyncList<string> activeIDs = new SyncList<string>();
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
			activeIDs.Callback += OnActiveIdListChange;
			for (int index = 0; index < activeIDs.Count; index++)
			{
				OnActiveIdListChange(SyncList<string>.Operation.OP_ADD, index, "", activeIDs[index]);
			}
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
			netIdentity.isDirty = true;
			Loggy.LogError($"[PlayerParticle/ToggleParticle] - no such object named {id}.");
		}

		public void ChangeStateOfParticle(string id, bool state)
		{
			foreach (var particle in particles.Where(x => x.ID == id))
			{
				particle.Reference.SetActive(state);
			}
		}

		public void DisableAllParticles()
		{
			foreach (var particle in particles)
			{
				particle.Reference.SetActive(false);
			}
		}

		private void OnActiveIdListChange(SyncList<string>.Operation op, int index, string oldItem, string newItem)
		{
			if (particles == null || particles.Count == 0) return;
			switch (op)
			{
				case SyncList<string>.Operation.OP_ADD:
					ChangeStateOfParticle(newItem, true);
					break;
				case SyncList<string>.Operation.OP_CLEAR:
					DisableAllParticles();
					break;
				case SyncList<string>.Operation.OP_INSERT:
					ChangeStateOfParticle(newItem, true);
					break;
				case SyncList<string>.Operation.OP_REMOVEAT:
					ChangeStateOfParticle(oldItem, false);
					break;
				case SyncList<string>.Operation.OP_SET:
					ChangeStateOfParticle(newItem, true);
					break;
				default:
					ChangeStateOfParticle(newItem, false);
					break;
			}
			netIdentity.isDirty = true;
		}
	}

	public class PlayerParticleObject
	{
		public GameObject Reference;
		public string ID => Reference.name;
	}
}