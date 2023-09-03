using System;
using System.Collections;
using Logs;
using UnityEngine;
using Mirror;

namespace Systems.Mob
{
	public class SimpleAnimal : LivingHealthBehaviour
	{
		private RegisterObject registerObject;
		private MobSprite mobSprite;

		//Syncvar hook so that new players can sync state on start
		[NonSerialized]
		[SyncVar(hook = nameof(SyncAliveState))]
		public bool deadState;

		[Server]
		public void SetDeadState(bool isDead)
		{
			deadState = isDead;
		}

		public override void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			mobSprite = GetComponent<MobSprite>();
		}

		private void Start()
		{
			OnClientFireStacksChange.AddListener(OnClientFireChange);
			OnClientFireChange(FireStacks);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncAliveState(deadState, deadState);
		}

		[Server]
		protected override void OnDeathActions()
		{
			deadState = true;
		}

		private void SyncAliveState(bool oldState, bool state)
		{
			deadState = state;

			if (mobSprite == null)
			{
				Loggy.LogError($"No {nameof(MobSprite)} component on {this}!", Category.Mobs);
				return;
			}

			if (state)
			{
				mobSprite.SetToDead();
				mobSprite.SetToBodyLayer();
				registerObject.SetPassable(true,state);
			}
			else
			{
				mobSprite.SetToAlive();
				mobSprite.SetToNPCLayer();
			}
		}

		private void OnClientFireChange(float fireStacks)
		{
			UpdateBurningOverlays(fireStacks);
		}

		private void UpdateBurningOverlays(float fireStacks)
		{
			if (mobSprite == null)
			{
				Loggy.LogError($"No {nameof(MobSprite)} component on {this}!", Category.Mobs);
				return;
			}

			if (fireStacks > 0)
			{
				mobSprite.SetBurningOverlay();
			}
			else
			{
				mobSprite.ClearBurningOverlay();
			}
		}
	}
}
