using System;
using Audio.Containers;
using CameraEffects;
using Chemistry;
using Core.Utils;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Items.Implants.Organs
{
	public class Brain : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect, IPlayerPossessable
	{

		public IPlayerPossessable Itself => this as IPlayerPossessable;
		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect)this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;
		[SyncVar(hook = nameof(SyncPossessingID))] private uint possessingID;

		[SerializeField] private Reagent DrunkReagent;
		[SerializeField] public float MaxDrunkAtPercentage = 0.06f;

		public uint OnPlayerID => OnBodyID;
		public uint PossessingID => possessingID;

		[FormerlySerializedAs("hasInbuiltSite")][SerializeField] private bool hasInbuiltSight = false;
		[SerializeField] private bool hasInbuiltHearing = false;


		[SerializeField] private bool CannotSpeak = false;


		[SerializeField] private bool hasInbuiltSpeech = false;
		//stuff in here?
		//nah


		[SyncVar(hook = nameof(SyncTelekinesis))] private bool hasTelekinesis = false;

		[SyncVar(hook = nameof(SyncDrunkenness))] private float drunkAmount = 0;

		public float DrunkAmount => drunkAmount;

		public bool HasTelekinesis => hasTelekinesis;

		public ChatModifier BodyChatModifier = ChatModifier.None;

		public ReagentCirculatedComponent ReagentCirculatedComponent;

		public UnityEvent OnDeath = new UnityEvent();
		public UnityEvent OnRevival = new UnityEvent();

		[RightClickMethod]
		public void Possess()
		{
			if (isServer)
			{
				PlayerManager.LocalMindScript.SetPossessingObject(this.gameObject);
			}
			else
			{
				PlayerManager.LocalMindScript.CmdRequestPossess(this.gameObject.NetId());
			}
		}


		public override void Awake()
		{
			base.Awake();
			RelatedPart = this.GetComponentCustom<BodyPart>();
			ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}

		public void Start()
		{
			SyncOnPlayer(this.netId, this.netId);
		}

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			RelatedPart.HealthMaster.SetBrain(this);
		}

		public void OnDestroy()
		{
			Itself.PreImplementedOnDestroy();
			OnDeath.RemoveAllListeners();
			OnRevival.RemoveAllListeners();
		}

		//Ensure removal of brain

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.SetBrain(this);
			Itself.SetPossessingObject(livingHealth.gameObject);

			if (CannotSpeak == false && hasInbuiltSpeech == false) return;

			if (hasInbuiltSpeech)
			{
				livingHealth.IsMute.RecordPosition(this, false);
			}
			else
			{
				livingHealth.IsMute.RecordPosition(this, CannotSpeak);
			}

			UpdateChatModifier(true);
			livingHealth.OnDeath += DeathEvent;
			livingHealth.OnRevive.AddListener(ReviveEvent);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			livingHealth.OnDeath -= DeathEvent;
			livingHealth.OnRevive.RemoveListener(ReviveEvent);
			if (livingHealth.brain == this)
			{
				livingHealth.SetBrain(null);
			}

			livingHealth.IsMute.RemovePosition(this);
			Itself.SetPossessingObject(null);
			UpdateChatModifier(false);
		}

		public void SyncTelekinesis(bool Oldvalue, bool NewValue)
		{
			hasTelekinesis = NewValue;
		}

		public void SyncDrunkenness(float Oldvalue, float NewValue)
		{
			drunkAmount = NewValue;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesDrunkenness(drunkAmount);
			}

		}

		public void ApplyChangesDrunkenness(float newState)
		{
			Camera.main.GetComponent<CameraEffectControlScript>().drunkCamera.SetDrunkStrength(newState);
		}

		public UnityEvent OnBodyUnPossesedByPlayer { get; set; }

		public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
		{
			possessingID = currentlyPossessing;
			Itself.PreImplementedSyncPossessingID(previouslyPossessing, currentlyPossessing);
		}

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}


		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
		}

		public override void ImplantPeriodicUpdate()
		{
			if (ReagentCirculatedComponent.OrNull()?.AssociatedSystem != null && ReagentCirculatedComponent.AssociatedSystem.BloodPool.reagents.Contains(DrunkReagent))
			{
				float DrunkPercentage = ReagentCirculatedComponent.AssociatedSystem.BloodPool.GetPercent(DrunkReagent);
				if (DrunkPercentage > 0)
				{
					if (DrunkPercentage > MaxDrunkAtPercentage)
					{
						DrunkPercentage = MaxDrunkAtPercentage;
					}
					var percentage = DrunkPercentage / MaxDrunkAtPercentage;

					if (percentage > 0.05f)
					{
						SyncDrunkenness(drunkAmount, percentage);
					}
					else
					{
						SyncDrunkenness(drunkAmount, 0);
					}
				}
				else
				{
					drunkAmount = 0;
				}
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
			ApplyChangesDrunkenness(Default ? 0 : drunkAmount);
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

		public void UpdateChatModifier(bool add)
		{
			if (RelatedPart.HealthMaster == null) return;
			if (add)
			{
				RelatedPart.HealthMaster.BodyChatModifier |= BodyChatModifier;
			}
			else
			{
				RelatedPart.HealthMaster.BodyChatModifier &= ~BodyChatModifier;
			}
		}

		private void DeathEvent()
		{
			OnDeath?.Invoke();
		}

		private void ReviveEvent()
		{
			OnRevival?.Invoke();
		}

		#region Mind_stuff

		public GameObject GameObject => gameObject;

		public IPlayerPossessable Possessing { get; set; }

		public GameObject PossessingObject { get; set; }

		public Mind PossessingMind { get; set; }

		public IPlayerPossessable PossessedBy { get; set; }

		[field: SerializeField] public MindNIPossessingEvent OnPossessedBy { get; set; } = new MindNIPossessingEvent();

		public Action OnActionControlPlayer { get; set; }

		public Action OnActionPossess { get; set; }
		public UnityEvent OnBodyPossesedByPlayer { get; set; }

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		public void OnControlPlayer(Mind mind) { }
		public void OnPossessPlayer(Mind mind, IPlayerPossessable parent) { }
		#endregion
	}
}