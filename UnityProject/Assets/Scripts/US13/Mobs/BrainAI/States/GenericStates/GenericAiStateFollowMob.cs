using UnityEngine;
using US13.Core.Addressables.Types;
using US13.HealthV2.Living;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Mobs.Traversal;
using Util;

namespace US13.Mobs.BrainAI.States.GenericStates
{
	/// <summary>
	/// A generic AI state that allows a mob to follow another mob. Will pathfind automatically once the target is far enough.
	/// </summary>
	public class GenericAiStateFollowMob : BrainMobState
	{
		[SerializeField] private AddressableAudioSource stateEnterSound;
		[SerializeField] private AddressableAudioSource stateExitSound;
		[SerializeField] private float minimumDistanceBeforePathfinding = 4f;

		private MobTraversal pathfinder => master.Traversal;
		[HideInInspector] public LivingHealthMasterBase MobToFollow = null;

		public override void OnEnterState()
		{
			if (stateEnterSound != null) SoundManager.PlayNetworkedAtPos(stateEnterSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
		}

		public override void OnExitState()
		{
			if (stateExitSound != null) SoundManager.PlayNetworkedAtPos(stateExitSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
		}

		public override void OnUpdateTick()
		{
			if (MobToFollow == null)
			{
				master.RemoveAddState(this, null);
				return;
			}
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit)
			{
				return;
			}
			if (Vector2.Distance(MobToFollow.gameObject.TileLocalPosition(),
				    LivingHealthMaster.gameObject.TileLocalPosition()) > minimumDistanceBeforePathfinding)
			{
				MobTraversal.TraversalDetails newDetails = new MobTraversal.TraversalDetails
				{
					TargetPosition = MobToFollow.gameObject.TileLocalPosition().To3Int(),
					CancelOnSlip = true
				};
				UpdateManager.ThinkShot(() => _ = pathfinder.CancelQueueAndGenerateNewPathToFollow(newDetails), PeriodicUpdateInterval - 0.5f);
			}
		}

		public override bool HasGoal()
		{
			return MobToFollow is not null;
		}
	}
}
