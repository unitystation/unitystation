using HealthV2;
using UnityEngine;

namespace Items.Implants.Organs.Vomit.LogicExtensions
{
	public class CureToxinOnVomit : MonoBehaviour, IVomitExtension
	{

		[SerializeField] private Vector2 minMaxToxinHeal = new Vector2(2, 6);

		public void OnVomit(float amount, LivingHealthMasterBase health, Stomach stomach)
		{
			health.HealDamageOnAll(null,
				Random.Range(minMaxToxinHeal.x, minMaxToxinHeal.y),
				DamageType.Tox);
		}
	}
}