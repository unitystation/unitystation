using Changeling;
using Mirror;
using ScriptableObjects.Systems.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Action;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	[Serializable]
	[DisallowMultipleComponent]
	public class ChangelingAbility : NetworkBehaviour, IActionGUI
	{
		public ChangelingData ability;

		public ChangelingData AbilityData => ability;

		//ActionData IActionGUI.ActionData => ability;

		public ActionData ActionData => ability;

		public virtual void CallActionClient()
		{
			UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilites(AbilityData.Index, action.LastClickPosition);
		}

		public void CallActionServer(PlayerInfo SentByPlayer, Vector3 clickPosition)
		{
			if (ValidateAbility(SentByPlayer) &&
				CastAbilityServer(SentByPlayer, clickPosition))
			{
				AfterAbility(SentByPlayer);
			}
		}

		private void AfterAbility(PlayerInfo sentByPlayer)
		{

		}

		private bool CastAbilityServer(PlayerInfo sentByPlayer, Vector3 clickPosition)
		{
			ability.PerfomAbility(sentByPlayer.Mind.Body.GetComponent<ChangelingMain>(), null);
			return true;
		}

		private bool ValidateAbility(PlayerInfo sentByPlayer)
		{
			var changelingMain = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			return changelingMain.HasAbility(this);
		}
	}

}