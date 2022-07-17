using HealthV2;
using Objects;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Health.Sickness
{
	public class Contagion: EnterTileBase
	{
		public Sickness Sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime = 20;

		private float spawnedTime;

		private RegisterTile registerTile;

		protected override void Awake()
		{
			base.Awake();
			registerTile = GetComponent<RegisterTile>();
		}

		public void Start()
		{
			spawnedTime = Time.time;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		//Server Side Only
		private void UpdateMe()
		{
			// Check if the contagion zone should despawn itself (after a set amount of time).
			// One day, we should hook this with the air scrubbers and general atmos system
			if (Time.time > spawnedTime + contagionTime)
			{
				_ = Despawn.ServerSingle(gameObject);
			}
		}

		/// <summary>
		/// If we want to see where the contagion is
		/// </summary>
		void OnDrawGizmos()
		{
			DebugGizmoUtils.DrawText(Sickness.SicknessName, registerTile.WorldPositionServer);
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return playerScript.PlayerState == PlayerScript.PlayerStates.Normal;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			SpawnResult sickNessResult = Spawn.ServerPrefab(transform.GetChild(0).gameObject, Vector3.zero, playerScript.gameObject.transform);
			if (sickNessResult.Successful && sickNessResult.GameObject.TryGetComponent<Sickness>(out var sickness))
			{
				sickness.SetCure(transform.GetChild(0).GetComponent<Sickness>().CureForSickness);
				playerScript.playerHealth.AddSickness(sickness);
			}
		}

		public void Setup(GameObject sicknessObject)
		{
			sicknessObject.transform.SetParent(this.transform);
		}
	}
}
