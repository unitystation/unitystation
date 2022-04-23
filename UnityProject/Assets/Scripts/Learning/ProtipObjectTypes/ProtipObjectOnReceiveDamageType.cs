using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnReceiveDamageType : ProtipObject
	{
		[SerializeField] private DamageType damageTypeThatTriggersTip;

		public void Start()
		{
			PlayerManager.PlayerScript.playerHealth.OnTakeDamageType += DamageTypeSimilar;
		}

		private void DamageTypeSimilar(DamageType type)
		{
			if(type == damageTypeThatTriggersTip) TriggerTip();
		}
	}
}