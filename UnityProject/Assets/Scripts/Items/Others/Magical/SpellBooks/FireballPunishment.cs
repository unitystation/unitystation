using System.Collections;
using Logs;
using UnityEngine;
using Systems.Explosions;

namespace Items.Magical
{
	/// <summary>
	/// Creates an explosion centered on the player.
	/// </summary>
	public class FireballPunishment : SpellBookPunishment
	{
		[SerializeField]
		private GameObject explosionPrefab = default;

		public override void Punish(PlayerInfo player)
		{
			GameObject explosionObject = Spawn.ServerPrefab(explosionPrefab, player.Script.WorldPos).GameObject;
			if (explosionObject.TryGetComponent<ExplosionComponent>(out var explosion))
			{
				explosion.Explode();
			}
			else
			{
				Loggy.LogError($"No explosion component found on {explosionObject}! Was the right prefab assigned?", Category.Spells);
			}
		}
	}
}
