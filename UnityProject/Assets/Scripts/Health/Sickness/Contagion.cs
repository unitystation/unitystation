using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	public class Contagion: MonoBehaviour
	{
		private Sickness sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime;

		private float spawnedTime;

		private RegisterTile registerTile;

		private Matrix tileMatrix => registerTile.Matrix;

		public void Start()
		{
			spawnedTime = Time.time;
		}

		public void Update()
		{
			// Check if the contagion zone should despawn itself (after a set amount of time).
			// One day, we should hook this with the air scrubbers and general atmos system
			if (Time.time > spawnedTime + contagionTime)
			{

			}
		}

		public void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (Validations.IsTarget(gameObject, interaction)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			DetectObjectsOnTile();
		}


		public void DetectObjectsOnTile()
		{
			if (!CustomNetworkManager.IsServer) return;

			var registerTileLocation = registerTile.LocalPositionServer;

			//detect players positioned on the portal bit of the gateway
			var playersFound = tileMatrix.Get<ObjectBehaviour>(registerTileLocation, ObjectType.Player, true);

			foreach (ObjectBehaviour player in playersFound)
			{
				Chat.AddGameWideSystemMsgToChat($"Ilayer {player.name} inside contagion zone!");
			}
		}
	}
}
