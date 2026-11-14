using System.Collections;
using Mirror;
using UnityEngine;
using US13.Core.Utils;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Items.Implants.Organs
{
	[System.Serializable]
	public class Ears : BodyPartBehavior, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{

		[SerializeField]
		private float localChatRange = 14;

		public float DefaultHearing = 1;

		public float PressureMultiplier = 1;
		public float EfficiencyMultiplier => RelatedPart.TotalModified;
		public float MutationMultiplier = 1;


		[SerializeField, UnityEngine.Serialization.FormerlySerializedAs("TotalMultiplier")] private float _networkTotalMultiplier = 1;
		public float TotalMultiplier
		{
			get => _networkTotalMultiplier;
			set
			{
				if (System.Collections.Generic.EqualityComparer<float>.Default.Equals(_networkTotalMultiplier, value)) return;
				var previous = _networkTotalMultiplier;
				_networkTotalMultiplier = value;
				if (isClient) ApplyChangesDeafness(previous, value);
			}
		}

		public Pickupable Pickupable;
		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;
		public ItemTrait DeafenProtection;

		private float deafenMultiplier = 1;
		private Coroutine deafenCoroutine;

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

		public uint OnPlayerID => OnBodyID;

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			_networkOnBodyID = CurrentlyOn;
			if (isClient) Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		public override void OnInitialize()
		{
			base.OnInitialize();
			Pickupable = this.GetComponent<Pickupable>();
			RelatedPart.ModifierChange += UpDateTotalValue;
		}

		public override void OnBehaviorDestroyed()
		{
			if (RelatedPart != null) RelatedPart.ModifierChange -= UpDateTotalValue;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckPressure);
		}

		public bool IsValidSetup(RegisterPlayer player)
		{
			if (player == null) return false;
			//Valid if with an organ storage?
			//yeah
			if (Pickupable.ItemSlot == null) return false;

			if (player.PlayerScript.playerHealth.BodyPartStorage !=
			    Pickupable.ItemSlot.ItemStorage.GetRootStorage()) return false;

			//Am I also in the organ storage? E.G Part of the body
			if (RelatedPart.HealthMaster == null) return false;


			//Loggy.LogError("IsValidSetup");
			return true;
		}

		public bool TryDeafen(GameObject sender, float deafenDuration, bool checkForProtectiveCloth = true)
		{
			if (RelatedPart.ItemAttributes.HasTrait(DeafenProtection))
			{
				return false;
			}

			if (checkForProtectiveCloth)
			{
				if (HasProtectiveCloth())
				{
					return false;
				}
			}

			RelatedPart.TakeDamage(sender, deafenDuration * 0.5f, AttackType.Energy, DamageType.Burn);

			DeafenPlayer(CurrentlyOn.netIdentity.connectionToClient, deafenDuration);
			return true;
		}

		public bool HasProtectiveCloth()
		{
			var playerStorage = RelatedPart.HealthMaster.playerScript.DynamicItemStorage;
			if (playerStorage == false) return false;

			foreach (var slots in playerStorage.ServerContents)
			{
				if (slots.Key != NamedSlot.ear && slots.Key != NamedSlot.head) continue;
				foreach (ItemSlot onSlots in slots.Value)
				{
					if (onSlots.IsEmpty) continue;
					if (onSlots.ItemAttributes.HasTrait(DeafenProtection))
					{
						return true;
					}
				}
			}
			return false;
		}

		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
			if (ShowForPlayer != null)
			{
				OnBodyID = ShowForPlayer.netId;
			}
			else
			{
				OnBodyID = NetId.Empty;
			}
		}



		public void ApplyDefaultOrCurrentValues(bool Default)
		{
			ApplyDeafness(Default, Default ? DefaultHearing : TotalMultiplier);
		}


		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckPressure);
			(livingHealth as PlayerHealthV2).OrNull()?.playerScript.PlayerStats.RemoveModifier(PlayerStats.Stat.LocalChatRange, gameObject.name);
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			UpdateManager.Add(CheckPressure, 1);
			(livingHealth as PlayerHealthV2).OrNull()?.playerScript.PlayerStats.AddModifier(PlayerStats.Stat.LocalChatRange, gameObject.name, localChatRange);
		}

		private void CheckPressure()
		{
			if (RelatedPart.HealthMaster == null)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckPressure);
				return;
			}

			var localPosition = Matrix.GetLocalPositionFromRootObject(Pickupable.UniversalObjectPhysics);
			var pressure =
				SweetExtensions.RegisterTile(RelatedPart.HealthMaster.playerScript.playerMove.GetRootObject)
					.Matrix.GetMetaDataNode(localPosition)
					.GasMixLocal.Pressure;

			if (pressure > 80)
			{
				if (Mathf.Approximately(PressureMultiplier, 1)) return;
				PressureMultiplier = 1;
				UpDateTotalValue();
			}
			else
			{
				var inModifier = pressure / 80f;
				if (Mathf.Approximately(inModifier, PressureMultiplier) == false) return;
				PressureMultiplier = pressure / 80f;
				UpDateTotalValue();
			}
		}

		public void ApplyChangesDeafness(float Oldv, float Newv)
		{
			_networkTotalMultiplier = Newv;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyDeafness(false,TotalMultiplier);
			}
		}

		[NaughtyAttributes.Button()]

		public void UpDateTotalValue()
		{
			ApplyChangesDeafness(TotalMultiplier, PressureMultiplier * MutationMultiplier * EfficiencyMultiplier * deafenMultiplier);
		}

		public void DeafenPlayer(NetworkConnection target, float deafenLength)
		{
			Owner.SendEvent(this, "deafen", deafenLength.ToString(System.Globalization.CultureInfo.InvariantCulture), target);
		}

		public override void OnNetworkEvent(string eventName, string payload)
		{
			if (eventName != "deafen") return;
			float deafenLength = float.Parse(payload, System.Globalization.CultureInfo.InvariantCulture);
			DeafenLocally(deafenLength);
		}

		private void DeafenLocally(float deafenLength)
		{
			if (deafenCoroutine != null) StopCoroutine(deafenCoroutine);
			deafenCoroutine = StartCoroutine(TemporaryDeafen(deafenLength));
		}

		private IEnumerator TemporaryDeafen(float deafenLength)
		{
			deafenMultiplier = 0;
			UpDateTotalValue();
			yield return new WaitForSeconds(deafenLength);
			deafenMultiplier = 1;
			UpDateTotalValue();
		}

		public void ApplyDeafness(bool Default, float Value)
		{
			if (Default)
			{
				AudioManager.Instance.MultiInterestFloat.RemovePosition(this);
			}
			else
			{
				AudioManager.Instance.MultiInterestFloat.RecordPosition(this , Value);
			}

		}
		[System.Serializable]
		private sealed class BehaviorNetworkState
		{
			public float TotalMultiplier;
			public uint OnBodyID;
		}

		public override string CaptureNetworkState() => JsonUtility.ToJson(new BehaviorNetworkState
		{
			TotalMultiplier = this.TotalMultiplier,
			OnBodyID = this.OnBodyID,
		});

		public override void ApplyNetworkState(string json)
		{
			var state = JsonUtility.FromJson<BehaviorNetworkState>(json);
			TotalMultiplier = state.TotalMultiplier;
			OnBodyID = state.OnBodyID;
		}

	}
}
