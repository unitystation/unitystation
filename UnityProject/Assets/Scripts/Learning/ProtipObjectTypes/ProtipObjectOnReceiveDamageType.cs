using System;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnReceiveDamageType : ProtipObject
	{
		[SerializeField] private DamageType damageTypeThatTriggersTip;

		public void OnEnable()
		{
			PlayerManager.PlayerScript.playerHealth.OnTakeDamageType += DamageTypeSimilar;
		}

		public void OnDisable()
		{
			PlayerManager.PlayerScript.playerHealth.OnTakeDamageType -= DamageTypeSimilar;
		}

		private void DamageTypeSimilar(DamageType type)
		{
			if(type == damageTypeThatTriggersTip) TriggerTip(gameObject);
		}
	}
}