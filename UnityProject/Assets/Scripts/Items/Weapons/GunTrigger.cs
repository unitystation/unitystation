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

		[Server]
		public TriggerBehaviour TriggerPull(GameObject shotBy)
		{
			if (playHONK)
			{
				AudioSourceParameters hornParameters = new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.7f, 1.2f));
				SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.ClownHonk, shotBy.AssumedWorldPosServer(),
					hornParameters, true, sourceObj: shotBy);
			}

			if (alwaysFail)
			{
				return TriggerBehaviour.None;
			}

			JobType job = PlayerList.Instance.Get(shotBy).Job;

			if (job == setRestriction || (setRestriction == JobType.NULL &&
			(job != JobType.CLOWN && allowNonClumsy || job == JobType.CLOWN && allowClumsy)))
			{
				// a normal shot
				return TriggerBehaviour.NormalShot;
			}
			else if (setRestriction == JobType.NULL && (job == JobType.CLOWN && !allowClumsy))
			{
				//shooting a non-clumsy weapon as a clumsy player
				return TriggerBehaviour.ClumsyShot;
			}
			else if (setRestriction == JobType.NULL && (job != JobType.CLOWN && !allowNonClumsy))
			{
				//shooting a clusmy only weapon as a non clumsy player
				return TriggerBehaviour.NonClumsyShot;
			}
			else
			{
				//Job restriction not met
				return TriggerBehaviour.None;
			}
		}
	}
}
