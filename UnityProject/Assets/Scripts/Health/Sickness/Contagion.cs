using UnityEngine;
using UnityEngine.EventSystems;

namespace Health.Sickness
{
	public class Contagion: MonoBehaviour
	{
		public Sickness Sickness;

		[SerializeField]
		[Tooltip("Time (in seconds) for the contagion to despawn itself")]
		private int contagionTime = 20;

		private float spawnedTime;

		private RegisterTile registerTile;
		
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
				// Despawns itself
				Despawn.ServerSingle(gameObject);
			}
		}

		public void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		/// <summary>
		/// Called from the Enterable component.  Handles what happens when a player enters the location of the contagion.
		/// </summary>
		public void OnEnterableEnter(BaseEventData eventData)
		{
			if (eventData.selectedObject.TryGetComponent(out PlayerHealth playerHealth))
			{
				playerHealth.AddSickness(Sickness);
			}
		}

		/// <summary>
		/// If we want to see where the contagion is
		/// </summary>
		void OnDrawGizmos()
		{
			DebugGizmoUtils.DrawText(Sickness.SicknessName, registerTile.WorldPositionServer);
		}
	}
}
