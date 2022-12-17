using System.Collections;
using System.Collections.Generic;
using Audio.Containers;
using Mirror;
using UnityEngine;

namespace HealthV2
{
	public class Brain : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }


		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;


		public Pickupable Pickupable;

		public uint OnPlayerID => OnBodyID;


		[SerializeField] private bool hasInbuiltSite = false;
		[SerializeField] private bool hasInbuiltHearing = false;

		//stuff in here?
		//nah

		public override void Awake()
		{
			base.Awake();
			RelatedPart = GetComponent<BodyPart>();
		}

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			RelatedPart.HealthMaster.Setbrain(this);
		}
		//Ensure removal of brain

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.Setbrain(this);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.brain = null;
		}

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
			if (ShowForPlayer != null)
			{
				SyncOnPlayer(OnBodyID, ShowForPlayer.netId);

			}
			else
			{
				SyncOnPlayer(OnBodyID, NetId.Empty);
			}
		}

		public bool IsValidSetup(RegisterPlayer player)
		{
			if (player == null) return false;
			//Valid if with an organ storage?

			//Am I also in the organ storage? E.G Part of the body
			if (RelatedPart.HealthMaster == null) return false;

			return true;
		}

		public void ApplyDefaultOrCurrentValues(bool Default)
		{
			ApplyChangesBlindness(Default ? false : true);
			ApplyDeafness(Default ? 0 : 1);
		}

		public void ApplyDeafness(float Value)
		{
			if (Value == 1)
			{
				AudioManager.Instance.MultiInterestFloat.RecordPosition(this, (!hasInbuiltHearing) ? 0f : 1f);
			}
			else
			{
				AudioManager.Instance.MultiInterestFloat.RemovePosition(this);
			}

		}

		public void ApplyChangesBlindness(bool SetValue)
		{
			if (SetValue)
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().Blindness.RecordPosition(this, !hasInbuiltSite);
			}
			else
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().Blindness.RemovePosition(this);
			}
		}
	}
}