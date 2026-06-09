using System;
using Logs;
using UnityEngine;
using US13.Core.Input_System;
using US13.Core.Transform;
using US13.HealthV2;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Projectiles;
using US13.Systems.Spells;
using US13.UI.Systems;

namespace US13.Systems.Spells
{
	public class MulticastHolder : Spell
	{
		//todo Make it so it just references another spell implementation, Pretty simple to do, You just pass through the CastSpellServer

		[SerializeField] private GameObject projectilePrefab = default;

		private int Used = 0;


		public int Uses = 3;

		public bool Active = false;

		public void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public override void CallActionClient()
		{
			Active = true;
			Used = 0;
		}


		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
		{
			base.CastSpellServer(caster, clickPosition, targetZone);

			if (TransformState.HiddenPos == clickPosition)
			{
				Used = 0;
				return true;
			}

			Vector3Int casterWorldPos = caster.Script.WorldPos;
			Vector2 castVector = clickPosition - casterWorldPos;
			ProjectileManager.InstantiateAndShoot(projectilePrefab, castVector,
				caster.Script.GameObject,
				null, targetZone);
			Used++;

			if (Used >= Uses)
			{
				Used = 0;
				return true;
			}


			return false;
		}


		public void UpdateMe()
		{
			if (Active == false) return;

			if (CommonInput.GetKey(KeyCode.Escape))
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestSpell(SpellData.Index,
					TransformState.HiddenPos, UIManager.DamageZone);
				Active = false;
				Used = 0;
				return;
			}


			if (CommonInput.GetMouseButtonDown(0))
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestSpell(SpellData.Index,
					CommonInput.CashedMouseWorldPosition, UIManager.DamageZone);

				Used++;
				if (Used >= Uses)
				{
					Used = 0;
					Active = false;
					return;
				}
			}
		}
	}
}