using UnityEngine;
using US13.HealthV2.Living;
using Util;

namespace US13.Items.Implants.Organs.Vomit.LogicExtensions
{
	public class StunOnVomit : MonoBehaviour, IVomitExtension
	{
		[SerializeField] private float stunDuration = 4f;
		public void OnVomit(float amount, LivingHealthMasterBase health, Stomach stomach)
		{
			health.playerScript.RegisterPlayer.ServerStun(stunDuration, true, false);
			if(DMMath.Prob(50)) health.playerScript.RegisterPlayer.ServerLayDown();
		}
	}
}