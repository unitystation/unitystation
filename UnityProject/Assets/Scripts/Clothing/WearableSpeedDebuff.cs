﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// when wore, this item will apply a debuff on player speed
	/// </summary>
	public class WearableSpeedDebuff : MonoBehaviour, IServerInventoryMove, PlayerMove.IMovementEffect
	{
		[SerializeField]
		[Tooltip("This will be the speed to substract from running speed")]
		private float runningSpeedDebuff = 1.5f;

		[SerializeField]
		[Tooltip("This will be speed to substract from walking speed")]
		private float walkingSpeedDebuff = 0.5f;


		public float RunningAdd {
			get => -runningSpeedDebuff;
			set { }
		}
		public float WalkingAdd {
			get => -walkingSpeedDebuff;
			set { }
		}
		public float CrawlAdd
		{
			get => 0;
			set{ }
		}



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
			else if (IsTakingOff(info))
			{
				RemoveDebuff();
			}
		}

		private bool IsPuttingOn (InventoryMove info)
		{
			if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
			{
				player = info.ToRootPlayer?.PlayerScript;

				if (player != null && info.ToSlot.NamedSlot == slot)
				{

					return true;
				}
			}

			return false;
		}

		private bool IsTakingOff (InventoryMove info)
		{
			if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
			{
				player = info.FromRootPlayer?.PlayerScript;

				if (player != null && info.FromSlot.NamedSlot == slot)
				{
					return true;
				}
			}

			return false;
		}

		void ApplyDebuff()
		{
			player.playerMove.AddModifier(this);
		}

		void RemoveDebuff()
		{
			player.playerMove.RemoveModifier(this);
		}
	}
}
