using HealthV2;
using Items.Implants.Organs;
using Mirror;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/StingAbility")]
	public class Sting: ChangelingBaseAbility
	{
		protected const float MAX_DISTANCE_TO_TILE = 1.6f;

		protected static readonly StandardProgressActionConfig stingProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);

		[SerializeField] protected float stingTime = 4f;

		public float StingTime => stingTime;

		protected PlayerScript GetPlayerOnClick(ChangelingMain changeling, Vector3 clickPosition, Vector3 rounded)
		{
			MatrixInfo matrixinfo = MatrixManager.AtPoint(rounded, true);
			clickPosition += new Vector3(-0.5f, -0.5f); // shifting point for geting player tile instead of shifted
			var tilePosition = matrixinfo.MetaTileMap.Layers[LayerType.Floors].Tilemap.WorldToCell(clickPosition);
			matrixinfo = MatrixManager.AtPoint(tilePosition, true);

			var localPosInt = clickPosition.ToLocal(matrixinfo.Matrix);

			PlayerScript target = null;
			foreach (PlayerScript integrity in matrixinfo.Matrix.Get<PlayerScript>(Vector3Int.CeilToInt(localPosInt), true))
			{
				// to be sure that player don`t morph into AI or something like that
				if (integrity.PlayerType != PlayerTypes.Normal || integrity.characterSettings.GetRaceSoNoValidation().Base.allowedToChangeling == false)
					continue;
				target = integrity;
				break;
			}
			if (target == null || target.Mind == null)
			{
				return null;
			}

			if (Vector3.Distance(changeling.ChangelingMind.Body.GameObject.AssumedWorldPosServer(), target.Mind.Body.GameObject.AssumedWorldPosServer()) > MAX_DISTANCE_TO_TILE)
			{
				return null;
			}

			return target;
		}

		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			return false;
		}

		[Server]
		public override bool UseAbilityServer(ChangelingMain changeling, Vector3 clickPosition)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			clickPosition = new Vector3(clickPosition.x, clickPosition.y, 0);
			var rounded = Vector3Int.RoundToInt(clickPosition);
			var target = GetPlayerOnClick(changeling, clickPosition, rounded);
			if (target == null || target == changeling.ChangelingMind.Body)
			{
				return false;
			}
			if (target.IsDeadOrGhost)
			{
				Chat.AddExamineMsg(changeling.ChangelingMind.gameObject, "<color=red>You cannot sting a dead body!</color>");
				return false;
			}

			changeling.UseAbility(this);
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You start sting of {target.visibleName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName} starts sting of {target.visibleName}</color>");

			var action = StandardProgressAction.Create(stingProgressBar,
				() => PerfomAbilityAfter(changeling, target));
			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, stingTime, changeling.ChangelingMind.Body.gameObject);
			return true;
		}

		protected virtual void PerfomAbilityAfter(ChangelingMain changeling, PlayerScript target)
		{
			var targetDNA = new ChangelingDna();
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You finished sting of {target.visibleName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName} finished sting of {target.visibleName}</color>");

			targetDNA.FormDna(target);

			changeling.AddDna(targetDNA);
		}
	}
}