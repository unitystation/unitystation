using Audio.Containers;
using HealthV2;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items.Implants.Organs
{
	public class Brain : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }


		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;


		public Pickupable Pickupable;

		public uint OnPlayerID => OnBodyID;


		[FormerlySerializedAs("hasInbuiltSite")] [SerializeField] private bool hasInbuiltSight = false;
		[SerializeField] private bool hasInbuiltHearing = false;


		[SerializeField] private bool CannotSpeak  = false;


		[SerializeField] private bool hasInbuiltSpeech = false;
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
			if (CannotSpeak == false && hasInbuiltSpeech == false) return;

			if (hasInbuiltSpeech)
			{
				livingHealth.IsMute.RecordPosition(this, false);
			}
			else
			{
				livingHealth.IsMute.RecordPosition(this, CannotSpeak);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.brain = null;
			livingHealth.IsMute.RemovePosition(this);
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
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().Blindness.RecordPosition(this, !hasInbuiltSight);
			}
			else
			{
				Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().Blindness.RemovePosition(this);
			}
		}

		public void SetCannotSpeak(bool inValue)
		{
			CannotSpeak = inValue;
			if (RelatedPart.HealthMaster == null) return;
			if (hasInbuiltSpeech)
			{
				RelatedPart.HealthMaster.IsMute.RecordPosition(this, false);
			}
			else
			{
				if (CannotSpeak)
				{
					RelatedPart.HealthMaster.IsMute.RecordPosition(this, CannotSpeak);
				}
				else
				{
					RelatedPart.HealthMaster.IsMute.RemovePosition(this);
					//Brain can't make you speak but it can stop you from speaking
				}
			}
		}
	}
}