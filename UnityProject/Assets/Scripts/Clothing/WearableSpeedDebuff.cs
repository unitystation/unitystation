using Player.Movement;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// when wore, this item will apply a debuff on player speed
	/// </summary>
	public class WearableSpeedDebuff : MonoBehaviour, IServerInventoryMove, IMovementEffect
	{
		[SerializeField]
		[Tooltip("This will be the speed to subtract from running speed")]
		private float runningSpeedDebuff = 1.5f;

		[SerializeField]
		[Tooltip("This will be speed to subtract from walking speed")]
		private float walkingSpeedDebuff = 0.5f;


		public float RunningSpeedModifier
		{
			get
			{
				if (SpeedDebuffRemoved) return 0;
				return -runningSpeedDebuff;
			}
		}

		public float WalkingSpeedModifier
		{
			get
			{
				if (SpeedDebuffRemoved) return 0;
				return -walkingSpeedDebuff;
			}
		}

		public float CrawlingSpeedModifier => 0;

		public bool SpeedDebuffRemoved = false;

		[SerializeField]
		[Tooltip("In what slot should this debuff take place")]
		private NamedSlot slot = NamedSlot.outerwear;

		private PlayerScript player;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (IsPuttingOn(info))
			{
				ApplyDebuff();
			}

			if (IsTakingOff(info))
			{
				RemoveDebuff();
			}
		}

		private bool IsPuttingOn (InventoryMove info)
		{
			if (info.ToSlot?.NamedSlot == null)
			{
				return false;
			}

			player = info.ToPlayer.OrNull()?.PlayerScript;

			return player != null && info.ToSlot.NamedSlot == slot;
		}

		private bool IsTakingOff (InventoryMove info)
		{
			if (info.FromSlot?.NamedSlot == null)
			{
				return false;
			}

			player = info.ToPlayer.OrNull()?.PlayerScript;

			return player != null && info.FromSlot.NamedSlot == slot;
		}

		private void ApplyDebuff()
		{
			player.playerMove.AddModifier(this);
		}

		private void RemoveDebuff()
		{
			player.playerMove.RemoveModifier(this);
		}
	}
}
