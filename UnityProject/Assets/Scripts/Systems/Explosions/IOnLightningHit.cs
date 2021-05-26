using UnityEngine;

namespace Systems.Explosions
{
	//Interface is triggered when lightning hits, allows for effects eg, tesla coil generating power
	public interface IOnLightningHit
	{
		void OnLightningHit(float duration, float damage);
	}
}
