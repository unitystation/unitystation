
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Spell that can be cast via UI action button in top left
/// </summary>
[Serializable]
public class Spell : MonoScript, IServerActionGUI
{

	public ActionData ActionData => SpellData;
	public SpellData SpellData =>
		spellData == null ?
			spellData = SpellList.GetDataForSpell(this) :
			spellData;
	[SerializeField] protected SpellData spellData = null;

	public int ChargesLeft
	{
		get => SpellData ? chargesLeft = SpellData.StartingCharges : chargesLeft;
		set => chargesLeft = value;
	}
	private int chargesLeft = 0;

	private Coroutine handle;

	public void CallActionClient()
	{
		//in the future: clientside cast info like cast click position
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestSpell(SpellData.index);
	}

	public void CallActionServer(ConnectedPlayer SentByPlayer)
	{
		if (ValidateCast(SentByPlayer) &&
		    CastSpellServer(SentByPlayer))
		{
			AfterCast(SentByPlayer);
		}
	}

	private void AfterCast(ConnectedPlayer sentByPlayer)
	{
		Cooldowns.TryStartServer(sentByPlayer.Script, CommonCooldowns.Instance.Spell, SpellData.CooldownTime);

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

			Chat.AddActionMsgToChat(sentByPlayer.GameObject, SpellData.InvocationMessageSelf,
				modPrefix+SpellData.InvocationMessage);
		}

		if (SpellData.ChargeType == SpellChargeType.FixedCharges && --ChargesLeft <= 0)
		{
			//remove it from spell list
			UIActionManager.Toggle(this, false, sentByPlayer.GameObject);
		}
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

		if ( castPosition == TransformState.HiddenPos)
		{ //got invalid position, aborting
			return false;
		}

		bool shouldDespawn = SpellData.SummonLifespan > 0f;

		//summon type
		if (SpellData.SummonType == SpellSummonType.Object)
		{
			foreach (var objectToSummon in SpellData.SummonObjects)
			{
				//spawn
				var spawnResult = Spawn.ServerPrefab(objectToSummon, castPosition);
				if (spawnResult.Successful && shouldDespawn)
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

				if (matrixInfo.MetaTileMap.HasTile(localPos, tileToSummon.LayerType, true)
				&& !SpellData.ReplaceExisting)
				{
					return false;
				}

				matrixInfo.TileChangeManager.UpdateTile(localPos, tileToSummon);
				if (shouldDespawn)
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

		if (caster.Script.IsDeadOrGhost)
		{
			return false;
		}

		bool isRecharging = Cooldowns.IsOnServer(caster.Script, CommonCooldowns.Instance.Spell);
		if (isRecharging)
		{
			Chat.AddExamineMsg(caster.GameObject, SpellData.StillRechargingMessage);
			return false;
		}

		//out of charges
		if (SpellData.ChargeType == SpellChargeType.FixedCharges && ChargesLeft < 1)
		{
			return false;
		}

		return true;
	}

}