using Logs;
using Player;
using UnityEngine;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/MapEditor")]
	public class MapEditor : GameMode
	{
		public GameObject EditorUIHolder;

		public override void SetupRound()
		{
			base.SetupRound();
			Spawn.ServerPrefab(EditorUIHolder);
		}

		public override bool IsPossible()
		{
			if (EditorUIHolder == null)
			{
				Loggy.Error("Missing UI Editor object, " +
				            "cannot enter mapping mode without the ability to save/load tilemap data via UI.");
				return false;
			}
			return true;
		}

		protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
		{
			return false;
		}
	}
}