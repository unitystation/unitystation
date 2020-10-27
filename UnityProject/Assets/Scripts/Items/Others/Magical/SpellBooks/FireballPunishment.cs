using UnityEngine;
using System.Collections;

namespace Items.Others.Magical
{
	/// <summary>
	/// Creates an explosion centered on the player.
	/// </summary>
	public class FireballPunishment : SpellBookPunishment
	{
		[SerializeField]
		private GameObject explosionPrefab = default;

		public override void Punish(ConnectedPlayer player)
		{
			Spawn.ServerPrefab(explosionPrefab, player.Script.WorldPos);
		}
	}
}
