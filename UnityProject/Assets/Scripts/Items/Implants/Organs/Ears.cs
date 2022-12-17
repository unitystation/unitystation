using Audio.Containers;
using HealthV2;
using Mirror;

namespace Items.Implants.Organs
{
	public class Ears : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{

		public float DefaultHearing = 1;

		public float PressureMultiplier = 1;
		public float EfficiencyMultiplier => RelatedPart.TotalModified;
		public float MutationMultiplier = 1;


		[SyncVar(hook = nameof(ApplyChangesDeafness))]
		public float TotalMultiplier = 1;

		public Pickupable Pickupable;
		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

		public uint OnPlayerID => OnBodyID;

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		public override void Awake()
		{
			base.Awake();
			Pickupable = this.GetComponent<Pickupable>();
			RelatedPart.ModifierChange += UpDateTotalValue;
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


			//Logger.LogError("IsValidSetup");
			return true;
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


		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckPressure);
		}

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			UpdateManager.Add(CheckPressure, 1);
		}

		private void CheckPressure()
		{

			if (RelatedPart.HealthMaster == null)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckPressure);
			}

			var Pressure =
				RelatedPart.HealthMaster.RegisterTile.Matrix.GetMetaDataNode(RelatedPart.HealthMaster.RegisterTile
					.LocalPosition).GasMix.Pressure;

			if (Pressure > 80)
			{
				if (PressureMultiplier != 1)
				{
					PressureMultiplier = 1;
					UpDateTotalValue();
				}
			}
			else
			{
				var InModifier = Pressure / 80f;
				if (InModifier != PressureMultiplier)
				{
					PressureMultiplier = Pressure / 80f;
					UpDateTotalValue();
				}

			}
		}

		public void ApplyChangesDeafness(float Oldv, float Newv)
		{
			TotalMultiplier = Newv;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyDeafness(false,TotalMultiplier);
			}
		}

		[NaughtyAttributes.Button()]

		public void UpDateTotalValue()
		{
			ApplyChangesDeafness(TotalMultiplier, PressureMultiplier * MutationMultiplier * EfficiencyMultiplier);
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
	}
}
