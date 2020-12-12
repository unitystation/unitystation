using UnityEngine;
using Mirror;

namespace Weapons
{
	public class GunTrigger : NetworkBehaviour
	{
		[SerializeField]
		private JobType setRestriction;
		[SerializeField]
		private bool allowClumsy;
		[SerializeField]
		private bool allowNonClumsy;

		[HideInInspector, SyncVar(hook = nameof(SyncPredictionCanFire))]
		public bool PredictionCanFire;

		public int TriggerPull(GameObject shotBy)
		{
			JobType job = PlayerList.Instance.Get(shotBy).Job;

			if (job == setRestriction || (setRestriction == JobType.NULL &&
			(job != JobType.CLOWN && allowNonClumsy || job == JobType.CLOWN && allowClumsy)))
			{
				// a normal shot
				return 2;
			}
			else if (setRestriction == JobType.NULL && (job == JobType.CLOWN && !allowClumsy))
			{
				//shooting a non-clusmy weapon as a clusmy person
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

		public bool TriggerPullClient()
		{
			return PredictionCanFire;
		}

		//Serverside method to update syncvar
		public void UpdatePredictionCanFire(GameObject holder)
		{
			JobType job = PlayerList.Instance.Get(holder).Job;
			if (holder == null)
			{
				SyncPredictionCanFire(PredictionCanFire, false);
			}
			else if (job == setRestriction || setRestriction == JobType.NULL)
			{
				SyncPredictionCanFire(PredictionCanFire, true);
			}
		}

		public void ClearPredictionCanFire()
		{
			SyncPredictionCanFire(PredictionCanFire, false);
		}

		/// <summary>
		/// Syncs the prediction bool
		/// </summary>
		private void SyncPredictionCanFire(bool oldValue, bool newValue)
		{
			PredictionCanFire = newValue;
		}

	}
}
