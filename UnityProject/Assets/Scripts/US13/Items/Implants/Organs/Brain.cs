using System;
using Chemistry;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Utils;
using US13.HealthV2.Living;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Managers;
using US13.Mobs.Traversal;
using US13.Player;
using US13.Player.MovementV2;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.RightClick;
using Util;

namespace US13.Items.Implants.Organs
{
	//NOTE SyncVar Only visible to owner!!!
	[System.Serializable]
	public class Brain : BodyPartBehavior, IItemInOutMovedPlayer, IClientSynchronisedEffect, IPlayerPossessable
	{
		public IPlayerPossessable Itself => this as IPlayerPossessable;
		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect)this;

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("OnBodyID")] private uint _networkOnBodyID;
		public uint OnBodyID
		{
			get => _networkOnBodyID;
			set
			{
				if (System.Collections.Generic.EqualityComparer<uint>.Default.Equals(_networkOnBodyID, value)) return;
				var previous = _networkOnBodyID;
				_networkOnBodyID = value;
				if (isClient) SyncOnPlayer(previous, value);
			}
		}
		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("possessingID")] private uint _networkPossessingID;
		private uint possessingID
		{
			get => _networkPossessingID;
			set
			{
				if (System.Collections.Generic.EqualityComparer<uint>.Default.Equals(_networkPossessingID, value)) return;
				var previous = _networkPossessingID;
				_networkPossessingID = value;
				if (isClient) SyncPossessingID(previous, value);
			}
		}

		[FormerlySerializedAs("DrunkReagent")] [SerializeField] private Reagent drunkReagent;
		[SerializeField] private Reagent highReagent;
		public Reagent DrunkReagent => drunkReagent;
		public Reagent HighReagent => highReagent;
		[SerializeField] public float MaxDrunkAtPercentage = 0.06f;
		[SerializeField] public float MaxHighAtPercentage = 0.06f;
		public uint OnPlayerID => OnBodyID;
		public uint PossessingID => possessingID;

		[FormerlySerializedAs("hasInbuiltSite")][SerializeField] private bool hasInbuiltSight = false;

		[SerializeField] private bool hasInbuiltHearing = false;
		[SerializeField] private bool CannotSpeak = false;
		[SerializeField] private bool hasInbuiltSpeech = false;
		//stuff in here?
		//nah

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("hasTelekinesis")] private bool _networkHasTelekinesis = false;
		private bool hasTelekinesis
		{
			get => _networkHasTelekinesis;
			set
			{
				if (System.Collections.Generic.EqualityComparer<bool>.Default.Equals(_networkHasTelekinesis, value)) return;
				var previous = _networkHasTelekinesis;
				_networkHasTelekinesis = value;
				if (isClient) SyncTelekinesis(previous, value);
			}
		}

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("drunkAmount")] private float _networkDrunkAmount = 0;
		private float drunkAmount
		{
			get => _networkDrunkAmount;
			set
			{
				if (System.Collections.Generic.EqualityComparer<float>.Default.Equals(_networkDrunkAmount, value)) return;
				var previous = _networkDrunkAmount;
				_networkDrunkAmount = value;
				if (isClient) SyncDrunkenness(previous, value);
			}
		}

		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("highAmount")] private float _networkHighAmount = 0;
		private float highAmount
		{
			get => _networkHighAmount;
			set
			{
				if (System.Collections.Generic.EqualityComparer<float>.Default.Equals(_networkHighAmount, value)) return;
				var previous = _networkHighAmount;
				_networkHighAmount = value;
				if (isClient) SyncHighness(previous, value);
			}
		}

		public MobTraversal Traversal => LivingHealthMaster?.playerScript.Traversal;

		public float DrunkAmount => drunkAmount;

		public bool HasTelekinesis => hasTelekinesis;

		public ChatModifier BodyChatModifier = ChatModifier.None;

		[UnityEngine.SerializeReference] public ReagentCirculatedComponent ReagentCirculatedComponent;

		public UnityEvent OnDeath = new UnityEvent();
		public UnityEvent OnRevival = new UnityEvent();

		public bool hasSillyWalk;
		public bool HasSillyWalk => hasSillyWalk;

		[RightClickMethod]
		public void Possess()
		{
			if (PlayerList.Instance.IsClientAdmin)
			{
				PlayerManager.LocalMindScript.SetPossessingObject(this.gameObject);
				if (isServer == false)
				{

					PlayerManager.LocalMindScript.CmdRequestPossess(this.gameObject.NetId());
				}
			}
		}

		public void SetSillyWalk(bool State)
		{
			hasSillyWalk = State;
			RecordPositionSillyWalk(hasSillyWalk);
		}


		public void RecordPositionSillyWalk(bool State)
		{
			if (LivingHealthMaster != null)
			{
				(LivingHealthMaster.ObjectBehaviour as MovementSynchronisation).ServerSillyWalk.RecordPosition(this, State);
			}
		}


		public override void OnInitialize()
		{
			base.OnInitialize();
			RelatedPart = gameObject.GetCachedComponent<BodyPart>();
			ReagentCirculatedComponent = GetBodyPartBehavior<ReagentCirculatedComponent>();
		}

		public override void OnBehaviorStarted()
		{
			SyncOnPlayer(this.netId, this.netId);
		}

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			RelatedPart.HealthMaster.SetBrain(this);
		}

		void IPlayerPossessable.OnDestroy() => OnBehaviorDestroyed();

		public override void OnBehaviorDestroyed()
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

			RecordPositionSillyWalk(hasSillyWalk);

			UpdateChatModifier(true);
			livingHealth.OnDeath += DeathEvent;
			livingHealth.OnRevive.AddListener(ReviveEvent);
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			PossessingMind?.Ghost(); //so Players can see explosions if they Self Bomb
			livingHealth.OnDeath -= DeathEvent;
			livingHealth.OnRevive.RemoveListener(ReviveEvent);
			if (livingHealth.brain == this)
			{
				livingHealth.SetBrain(null);
			}

			(LivingHealthMaster.ObjectBehaviour as MovementSynchronisation).ServerSillyWalk.RemovePosition(this);

			livingHealth.IsMute.RemovePosition(this);
			Itself.SetPossessingObject(null);
			UpdateChatModifier(false);


		}

		public void SyncTelekinesis(bool Oldvalue, bool NewValue)
		{
			_networkHasTelekinesis = NewValue;
		}

		public void SyncHighness(float Oldvalue, float NewValue)
		{
			_networkHighAmount = NewValue;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesHighness(highAmount);
			}
		}

		public void SyncDrunkenness(float Oldvalue, float NewValue)
		{
			_networkDrunkAmount = NewValue;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesDrunkenness(drunkAmount);
			}
		}

		public void ApplyChangesDrunkenness(float newState)
		{
			Camera.main.GetComponent<CameraEffectControlScript>().drunkCamera.SetDrunkStrength(newState);
		}

		public void ApplyChangesHighness(float newState)
		{
			Camera.main.GetComponent<CameraEffectControlScript>().HighCamera.SetStrength(newState);
		}

		public UnityEvent OnBodyUnPossesedByPlayer { get; set; } = new UnityEvent();

		public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
		{
			_networkPossessingID = currentlyPossessing;
			Itself.PreImplementedSyncPossessingID(previouslyPossessing, currentlyPossessing);
		}

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			_networkOnBodyID = CurrentlyOn;
			if (isClient) Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}


		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
		}

		public override void ImplantPeriodicUpdate()
		{
			if (drunkReagent != null) DrunkCheck();
			if (highReagent != null) HighCheck();
		}

		private void DrunkCheck()
		{
			if (ReagentCirculatedComponent?.AssociatedSystem == null) return;
			if (ReagentCirculatedComponent.AssociatedSystem.BloodPool.reagents.Contains(drunkReagent) == false) return;
			float drunkPercentage = ReagentCirculatedComponent.AssociatedSystem.BloodPool.GetPercent(drunkReagent);
			if (drunkPercentage > 0)
			{
				if (drunkPercentage > MaxDrunkAtPercentage)
				{
					drunkPercentage = MaxDrunkAtPercentage;
				}
				var percentage = drunkPercentage / MaxDrunkAtPercentage;

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

		private void HighCheck()
		{
			if (ReagentCirculatedComponent?.AssociatedSystem == null) return;
			if (ReagentCirculatedComponent.AssociatedSystem.BloodPool.reagents.Contains(highReagent) == false) return;
			float drunkPercentage = ReagentCirculatedComponent.AssociatedSystem.BloodPool.GetPercent(highReagent);
			if (drunkPercentage > 0)
			{
				if (drunkPercentage > MaxHighAtPercentage)
				{
					drunkPercentage = MaxHighAtPercentage;
				}
				var percentage = drunkPercentage / MaxHighAtPercentage;

				if (percentage > 0.05f)
				{
					SyncHighness(highAmount, percentage);
				}
				else
				{
					SyncHighness(highAmount, 0);
				}
			}
			else
			{
				highAmount = 0;
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
			ApplyChangesHighness(Default ? 0 : highAmount);
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
				Camera.main.GetComponent<CameraEffectControlScript>().Blindness.RecordPosition(this, !hasInbuiltSight);
			}
			else
			{
				Camera.main.GetComponent<CameraEffectControlScript>().Blindness.RemovePosition(this);
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
		public UnityEvent OnBodyPossesedByPlayer { get; set; } = new UnityEvent();

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		public void OnControlPlayer(Mind mind) { }

		public void OnPossessPlayer(Mind mind, IPlayerPossessable parent)
		{

		}
		#endregion
		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public uint OnBodyID;
			public uint possessingID;
			public bool hasTelekinesis;
			public float drunkAmount;
			public float highAmount;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			OnBodyID = this.OnBodyID,
			possessingID = this.possessingID,
			hasTelekinesis = this.hasTelekinesis,
			drunkAmount = this.drunkAmount,
			highAmount = this.highAmount,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			OnBodyID = state.OnBodyID;
			possessingID = state.possessingID;
			hasTelekinesis = state.hasTelekinesis;
			drunkAmount = state.drunkAmount;
			highAmount = state.highAmount;
		}

	}
}
