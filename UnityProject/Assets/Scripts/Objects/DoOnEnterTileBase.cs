using System.Collections.Generic;
using SecureStuff;
using UnityEngine;

namespace Objects
{
	public class DoOnEnterTileBase : EnterTileBase
	{
		[SerializeField] private List<SerializedAction> actionsOnStepToTrigger = new();

		public bool EnableForPlayers = true;
		public bool EnableForObjects = true;

		public override bool WillAffectObject(GameObject eventData)
		{
			return EnableForObjects;
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return EnableForPlayers;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			base.OnObjectEnter(eventData);
			foreach (var a in actionsOnStepToTrigger)
			{
				a?.Invoke(eventData);
			}
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			base.OnPlayerStep(playerScript);
			foreach (var a in actionsOnStepToTrigger)
			{
				a?.Invoke(playerScript.GameObject);
			}
		}
	}
}