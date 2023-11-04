using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logs;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	[Serializable]
	[DisallowMultipleComponent]
	public class ChangelingAbility : NetworkBehaviour, IActionGUI
	{
		public ChangelingBaseAbility ability;
		public ChangelingBaseAbility AbilityData => ability;
		public ActionData ActionData => ability;
		public float CooldownTime { get; set; }
		[SyncVar]
		private bool isToggled = false;
		public bool IsToggled => isToggled;

		public virtual void CallActionClient()
		{
			var action = UIActionManager.Instance.DicIActionGUI[this][0];

			if (UIManager.Instance.displayControl.hudChangeling.ChangelingMain == null)
				return;

			if (AbilityData.IsLocal)
			{
				if (ValidateAbilityClient())
				{
					AbilityData.UseAbilityClient(UIManager.Instance.displayControl.hudChangeling.ChangelingMain);
					if (AbilityData is ChangelingToggleAbility toggleAbili)
					{
						isToggled = !isToggled;
						toggleAbili.UseAbilityToggleClient(UIManager.Instance.displayControl.hudChangeling.ChangelingMain, isToggled);
					}
					AfterAbilityClient(PlayerManager.LocalPlayerScript);
				}
			} else
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilites(AbilityData.Index, action.LastClickPosition);

				if (AbilityData is ChangelingToggleAbility)
				{
					PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilites(AbilityData.Index, action.LastClickPosition);
				}
			}
		}

		[Server]
		public void CallActionServer(PlayerInfo SentByPlayer, Vector3 clickPosition)
		{
			if (AbilityData is ChangelingToggleAbility)
			{
				CallActionServerToggle(SentByPlayer, !IsToggled);
				return;
			}

			if (ValidateAbility(SentByPlayer) == false)
				return;
			if (CastAbilityServer(SentByPlayer, clickPosition))
			{
				AfterAbility(SentByPlayer);
			}
		}

		[Server]
		public void CallActionServerToggle(PlayerInfo sentByPlayer, bool toggle)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			var validateAbility = ValidateAbility(sentByPlayer);
			if (validateAbility && AbilityData is ChangelingToggleAbility toggleAbility)
			{
				if (toggle == true && toggleAbility.CooldownWhenToggled == true)
				{
					AfterAbility(sentByPlayer);
				}
				else
				{
					AfterAbility(sentByPlayer);
				}

				try
				{
					isToggled = toggle;
					toggleAbility.UseAbilityToggleServer(changeling, isToggled);
				}
				catch (Exception ex)
				{
					Loggy.LogError($"[ChangelingAbility/CallActionServerToggle] Failed to use ability {AbilityData.Name}. Toggle to was {toggle}." +
					$"Sent by player '{sentByPlayer.Username}' {ex}");
				}
			}
			if (ActionData.Sprites.Count != 2)
				return;
			if (isToggled)
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
			}
			else
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
			}
		}

		public void ForceToggleToState(bool toggle)
		{
			isToggled = toggle;
			if (ActionData.Sprites.Count != 2)
				return;
			if (isToggled)
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
			}
			else
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
			}
		}

		[Server]
		public void CallActionServerWithParam(PlayerInfo sentByPlayer, string paramString)
		{
			List<string> param = paramString.Split('\n').ToList();
			if (ValidateAbility(sentByPlayer) &&
				CastAbilityServerWithParam(sentByPlayer, param))
			{
				AfterAbility(sentByPlayer);
			}
		}

		[Server]
		private bool CastAbilityServerWithParam(PlayerInfo sentByPlayer, List<string> param)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];

			if (AbilityData is ChangelingParamAbility abilityParam)
			{
				try
				{
					abilityParam.UseAbilityParamServer(changeling, param);
				}
				catch (Exception ex)
				{
					var errorStringBuilder = new StringBuilder();
					foreach (var x in param)
					{
						errorStringBuilder.Append("'");
						errorStringBuilder.AppendLine(x);
						if (param.Last() != x)
							errorStringBuilder.Append("',");
					}
					Loggy.LogError($"[ChangelingAbility/CastAbilityServerWithParam] Failed to use ability {AbilityData.Name}. Params was: {errorStringBuilder}." +
					$"Sent by player '{sentByPlayer.Username}' {ex}");
					return false;
				}
				return true;
			}
			return false;
		}

		[Server]
		private bool CastAbilityServer(PlayerInfo sentByPlayer, Vector3 clickPosition)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			try
			{
				AbilityData.UseAbilityServer(changeling, clickPosition);
			} catch (Exception ex)
			{
				Loggy.LogError($"[ChangelingAbility/CastAbilityServer] Failed to use ability {AbilityData.Name}. Click position was {clickPosition}." +
				$"Sent by player '{sentByPlayer.Username}' {ex}");
				return false;
			}
			return true;
		}

		private void AfterAbility(PlayerInfo sentByPlayer)
		{
			if (CooldownTime < 0.01f)
				return;
			Cooldowns.TryStartServer(sentByPlayer.Script, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private void AfterAbilityClient(PlayerScript sentByPlayer)
		{
			if (CooldownTime < 0.01f)
				return;
			Cooldowns.TryStartClient(sentByPlayer, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private bool ValidateAbility(PlayerInfo sentByPlayer)
		{
			if (CustomNetworkManager.IsServer == false) return false;

			var changelingMain = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			if (sentByPlayer.Script.IsDeadOrGhost || (sentByPlayer.Script.playerHealth.IsCrit && !AbilityData.CanBeUsedWhileInCrit))
			{
				return false;
			}

			if (changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, "Not enough chemicals for ability!");
				return false;
			}

			bool isRecharging = Cooldowns.IsOnServer(sentByPlayer.Script, AbilityData);
			if (isRecharging)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, $"Ability {AbilityData.Name} is recharging!");
				return false;
			}
			return changelingMain.HasAbility(ability);
		}

		private bool ValidateAbilityClient()
		{
			var changelingMain = UIManager.Display.hudChangeling.ChangelingMain;
			if (changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, "Not enough chemicals for ability!");
				return false;
			}
			if (changelingMain.ChangelingMind.Body.IsDeadOrGhost || changelingMain.ChangelingMind.Body.playerHealth.IsCrit)
			{
				return false;
			}

			bool isRecharging = Cooldowns.IsOnClient(changelingMain.ChangelingMind.Body, AbilityData) ||
			Cooldowns.IsOnServer(changelingMain.ChangelingMind.Body, AbilityData);
			if (isRecharging)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, $"Ability {AbilityData.Name} is recharging!");
				return false;
			}

			return changelingMain.HasAbility(ability);
		}
	}
}