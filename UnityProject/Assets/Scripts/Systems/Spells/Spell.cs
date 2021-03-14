using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ScriptableObjects.Systems.Spells;

namespace Systems.Spells
{
	/// <summary>
	/// Spell that can be cast via UI action button in top left
	/// </summary>
	[Serializable]
	[DisallowMultipleComponent]
	public class Spell : NetworkBehaviour, IActionGUI
	{
		private SpellData spellData = null;
		public SpellData SpellData {
			get => spellData;
			set => spellData = value;
		}
		public ActionData ActionData => SpellData;

		public int CurrentTier { get; private set; } = 1;
		public float CooldownTime { get; set; }

		public int ChargesLeft {
			get => SpellData ? chargesLeft = SpellData.StartingCharges : chargesLeft;
			set => chargesLeft = value;
		}

		private int chargesLeft = 0;

		protected Coroutine handle;

		private void Awake()
		{
			if (SpellData == null)
			{
				SpellData = SpellList.Instance.InvalidData;
			}
		}

		public virtual void CallActionClient()
		{
			UIAction action = UIActionManager.Instance.DicIActionGUI[this];
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestSpell(SpellData.Index, action.LastClickPosition);
		}

		public void CallActionServer(ConnectedPlayer SentByPlayer, Vector3 clickPosition)
		{
			if (ValidateCast(SentByPlayer) &&
				CastSpellServer(SentByPlayer, clickPosition))
			{
				AfterCast(SentByPlayer);
			}
		}

		private void AfterCast(ConnectedPlayer sentByPlayer)
		{
			Cooldowns.TryStartServer(sentByPlayer.Script, SpellData, CooldownTime);

			SoundManager.PlayNetworkedAtPos(
					SpellData.CastSound, sentByPlayer.Script.WorldPos, sourceObj: sentByPlayer.GameObject, global: false);

			if (SpellData.InvocationType != SpellInvocationType.None)
			{
				string modPrefix = String.Empty;
				switch (SpellData.InvocationType)
				{
					case SpellInvocationType.Emote:
						modPrefix = "*";
						break;
					case SpellInvocationType.Whisper:
						modPrefix = "%";
						break;
				}

				if (sentByPlayer == null || sentByPlayer.CharacterSettings == null) return;

				Chat.AddActionMsgToChat(sentByPlayer.GameObject, FormatInvocationMessageSelf(sentByPlayer),
					FormatInvocationMessage(sentByPlayer, modPrefix));

				if (SpellData.InvocationType == SpellInvocationType.Shout)
				{
					Chat.AddChatMsgToChat(sentByPlayer, FormatInvocationMessage(sentByPlayer, modPrefix), ChatChannel.Local);
				}
			}

			if (SpellData.ChargeType == SpellChargeType.FixedCharges && --ChargesLeft <= 0)
			{
				//remove it from spell list
				UIActionManager.Toggle(this, false, sentByPlayer.GameObject);
			}
			else
			{
				UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
			}
		}

		public virtual bool CastSpellServer(ConnectedPlayer caster, Vector3 clickPosition)
		{
			return CastSpellServer(caster);
		}

		/// <returns>false if it was aborted for some reason</returns>
		public virtual bool CastSpellServer(ConnectedPlayer caster)
		{
			if (SpellData.SummonType == SpellSummonType.None)
			{ //don't want to summon anything physical and that's alright
				return true;
			}

			Vector3Int castPosition = TransformState.HiddenPos;

			Vector3Int casterPosition = caster.Script.AssumedWorldPos;
			switch (SpellData.SummonPosition)
			{
				case SpellSummonPosition.CasterTile:
					castPosition = casterPosition;
					break;
				case SpellSummonPosition.CasterDirection:
					if (casterPosition == TransformState.HiddenPos)
					{
						break;
					}
					castPosition = casterPosition + caster.Script.CurrentDirection.VectorInt.To3Int();
					break;
				case SpellSummonPosition.Custom:
					castPosition = GetWorldSummonPosition(caster);
					break;
			}

			if (castPosition == TransformState.HiddenPos)
			{ //got invalid position, aborting
				return false;
			}

			//summon type
			if (SpellData.SummonType == SpellSummonType.Object)
			{
				foreach (var objectToSummon in SpellData.SummonObjects)
				{
					//spawn
					var spawnResult = Spawn.ServerPrefab(objectToSummon, castPosition);
					if (spawnResult.Successful && SpellData.ShouldDespawn)
					{
						//but also destroy when lifespan ends
						caster.Script.StartCoroutine(DespawnAfterDelay(), ref handle);

						IEnumerator DespawnAfterDelay()
						{
							yield return WaitFor.Seconds(SpellData.SummonLifespan);
							Despawn.ServerSingle(spawnResult.GameObject);
						}
					}
				}
			}
			else if (SpellData.SummonType == SpellSummonType.Tile)
			{
				foreach (var tileToSummon in SpellData.SummonTiles)
				{
					//spawn
					var matrixInfo = MatrixManager.AtPoint(castPosition, true);
					var localPos = MatrixManager.WorldToLocalInt(castPosition, matrixInfo);

					if (matrixInfo.MetaTileMap.HasTile(localPos, tileToSummon.LayerType)
					&& !SpellData.ReplaceExisting)
					{
						return false;
					}

					matrixInfo.TileChangeManager.UpdateTile(localPos, tileToSummon);
					if (SpellData.ShouldDespawn)
					{
						//but also destroy when lifespan ends
						caster.Script.StartCoroutine(DespawnAfterDelay(), ref handle);

						IEnumerator DespawnAfterDelay()
						{
							yield return WaitFor.Seconds(SpellData.SummonLifespan);
							matrixInfo.TileChangeManager.RemoveTile(localPos, tileToSummon.LayerType, false);
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Override this in your subclass for custom logic
		/// </summary>
		public virtual Vector3Int GetWorldSummonPosition(ConnectedPlayer caster)
		{
			return TransformState.HiddenPos;
		}

		public virtual bool ValidateCast(ConnectedPlayer caster)
		{
			if (SpellData == null)
			{
				Logger.LogErrorFormat("Spell {0} initiated by {1}:\nSpellData is null!", Category.Spells, this, caster);
				return false;
			}

			if (!caster.Script.mind.Spells.Contains(this))
			{
				Logger.LogWarningFormat("Illegal spell access: {0} tried to call spell they don't possess ({1})",
					Category.Exploits, caster, this);
				return false;
			}

			if (caster.Script.IsDeadOrGhost || caster.Script.playerHealth.IsCrit)
			{
				return false;
			}

			bool isRecharging = Cooldowns.IsOnServer(caster.Script, SpellData);
			if (isRecharging)
			{
				Chat.AddExamineMsg(caster.GameObject, FormatStillRechargingMessage(caster));
				return false;
			}

			//out of charges
			if (SpellData.ChargeType == SpellChargeType.FixedCharges && ChargesLeft < 1)
			{
				return false;
			}

			if (SpellData is WizardSpellData data && data.RequiresWizardGarb && CheckWizardGarb(caster.Script.Equipment) == false)
			{
				return false;
			}

			return true;
		}

		private bool CheckWizardGarb(Equipment casterEquipment)
		{
			var outerwear = casterEquipment.ItemStorage.GetNamedItemSlot(NamedSlot.outerwear);
			if (outerwear.IsEmpty || outerwear.ItemAttributes.HasTrait(CommonTraits.Instance.WizardGarb) == false)
			{
				Chat.AddExamineMsg(casterEquipment.gameObject, "<color=red>You don't feel strong enough without your robe!</color>");
				return false;
			}

			var headwear = casterEquipment.ItemStorage.GetNamedItemSlot(NamedSlot.head);
			if (headwear.IsEmpty || headwear.ItemAttributes.HasTrait(CommonTraits.Instance.WizardGarb) == false)
			{
				Chat.AddExamineMsg(casterEquipment.gameObject, "<color=red>You don't feel strong enough without your hat!</color>");
				return false;
			}

			return true;
		}

		protected virtual string FormatInvocationMessage(ConnectedPlayer caster, string modPrefix)
		{
			return modPrefix + SpellData.InvocationMessage;
		}

		protected virtual string FormatInvocationMessageSelf(ConnectedPlayer caster)
		{
			return SpellData.InvocationMessageSelf;
		}
		protected virtual string FormatStillRechargingMessage(ConnectedPlayer caster)
		{
			return SpellData.StillRechargingMessage;
		}

		public virtual void UpgradeTier()
		{
			CurrentTier++;
			CooldownTime -= CooldownTime * (SpellData as WizardSpellData).CooldownModifier;
		}
	}
}
