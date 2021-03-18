using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/ShakeEmote")]
	public class ShakeEmote : EmoteSO
	{
		[SerializeField]
		private float shakeDuration = 1f;

		[SerializeField]
		private float shakeIntensity = 0.1f;

		[SerializeField]
		private float shakeDelay = 0.1f;

		public override void Do(GameObject player)
		{
			PlayerEffectsManager manager = player.GetComponent<PlayerEffectsManager>();
			manager.ShakePlayer(shakeDuration, shakeIntensity, shakeDelay);
			base.Do(player);
		}
	}
}

