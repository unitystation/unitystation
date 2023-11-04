using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnChangeIntent : ProtipObject
	{
		[SerializeField] private PlayerScript playerScript;

		[SerializeField] private ProtipSO HelpIntent;
		[SerializeField] private ProtipSO HarmIntent;
		[SerializeField] private ProtipSO DisarmIntent;

		protected override void Awake()
		{
			if (playerScript == null) playerScript = GetComponentInParent<PlayerScript>();
			playerScript.OnIntentChange += OnIntentChange;
		}

		private void OnDestroy()
		{
			if (playerScript != null) playerScript.OnIntentChange -= OnIntentChange;
		}

		private void OnIntentChange(Intent changedIntent)
		{
			switch (changedIntent)
			{
				case Intent.Help:
					TriggerTip(HelpIntent);
					break;
				case Intent.Disarm:
					TriggerTip(DisarmIntent);
					break;
				case Intent.Grab:
					break;
				case Intent.Harm:
					TriggerTip(HarmIntent);
					break;
			}
		}
	}
}