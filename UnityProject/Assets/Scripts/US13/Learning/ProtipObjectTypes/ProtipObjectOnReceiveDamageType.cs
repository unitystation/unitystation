using UnityEngine;
using US13.HealthV2;
using US13.Player;

namespace US13.Learning.ProtipObjectTypes
{
	public class ProtipObjectOnReceiveDamageType : ProtipObject
	{
		[SerializeField] private DamageType damageTypeThatTriggersTip;

		public void OnEnable()
		{
			PlayerManager.LocalPlayerScript.playerHealth.OnTakeDamageType += DamageTypeSimilar;
		}

		public void OnDisable()
		{
			PlayerManager.LocalPlayerScript.playerHealth.OnTakeDamageType -= DamageTypeSimilar;
		}

		private void DamageTypeSimilar(DamageType type, GameObject affector, float amount)
		{
			if(type == damageTypeThatTriggersTip) TriggerTip(gameObject);
		}
	}
}