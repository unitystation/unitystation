using Logs;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Systems.Explosions;

namespace US13.Items.Others.Magical.SpellBooks
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
				Loggy.Error($"No explosion component found on {explosionObject}! Was the right prefab assigned?", Category.Spells);
			}
		}
	}
}
