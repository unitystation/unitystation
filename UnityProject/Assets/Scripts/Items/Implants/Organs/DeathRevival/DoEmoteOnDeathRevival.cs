using HealthV2;
using ScriptableObjects.RP;
using UnityEngine;

namespace Items.Implants.Organs.DeathRevival
{
	/// <summary>
	/// Cause a player to emote when they become dead or revived. Use events to trigger functions.
	/// </summary>
	public class DoEmoteOnDeathRevival : MonoBehaviour
	{
		[SerializeField] private EmoteSO deathEmote;
		[SerializeField] private EmoteSO revivalEmote;

		[SerializeField] private BodyPart relatedBodyPart;


		private void Awake()
		{
			relatedBodyPart ??= GetComponent<BodyPart>();
		}

		public void Death()
		{
			deathEmote.Do(relatedBodyPart.HealthMaster.gameObject);
		}

		public void Revive()
		{
			revivalEmote.Do(relatedBodyPart.HealthMaster.gameObject);
		}
	}
}