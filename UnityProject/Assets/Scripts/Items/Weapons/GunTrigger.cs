using Messages.Server.SoundMessages;
using UnityEngine;
using Mirror;


namespace Weapons
{
	public class GunTrigger : NetworkBehaviour
	{
		[SerializeField]
		private JobType setRestriction;
		public JobType SetRestriction => setRestriction;
		[SerializeField]
		private bool allowClumsy;
		[SerializeField]
		private bool allowNonClumsy;
		[SerializeField]
		private bool alwaysFail;

		public string DeniedMessage;

		public bool playHONK; //honk.
		private float randomPitch => Random.Range( 0.7f, 1.2f );

		[Server]
		public int TriggerPull(GameObject shotBy)
		{
			if (playHONK)
			{
				AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: randomPitch);
				SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ClownHonk, shotBy.AssumedWorldPosServer(),
					hornParameters, true, sourceObj: shotBy);
			}

			if (alwaysFail)
			{
				return 0;
			}

			JobType job = PlayerList.Instance.Get(shotBy).Job;

			if (job == setRestriction || (setRestriction == JobType.NULL &&
			(job != JobType.CLOWN && allowNonClumsy || job == JobType.CLOWN && allowClumsy)))
			{
				// a normal shot
				return 2;
			}
			else if (setRestriction == JobType.NULL && (job == JobType.CLOWN && !allowClumsy))
			{
				//shooting a non-clumsy weapon as a clumsy person
				return 3;
			}
			else if (setRestriction == JobType.NULL && (job != JobType.CLOWN && !allowNonClumsy))
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}
}
