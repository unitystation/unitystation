﻿using System.Collections;
using UnityEngine;
using Systems.Explosions;

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
			GameObject explosionObject = Spawn.ServerPrefab(explosionPrefab, player.Script.WorldPos).GameObject;
			if (explosionObject.TryGetComponent<ExplosionComponent>(out var explosion))
			{
				explosion.Explode(MatrixManager.AtPoint(player.Script.WorldPos, true).Matrix);
			}
			else
			{
				Logger.LogError($"No explosion component found on {explosionObject}! Was the right prefab assigned?", Category.Spells);
			}
		}
	}
}
