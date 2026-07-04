using System;
using System.Collections.Generic;
using Chemistry;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Camera;
using US13.Core.Transform;
using US13.Core.Utils;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Objects.Directionals;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;
using Random = UnityEngine.Random;

namespace US13.Items.Implants.Organs
{
	public class Eye : BodyPartFunctionality, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{
		//TODO
		//Probably should make it so the shader has Multi-interest bool /  Think of a system to We never have this issue with Things overlap with what they control
		//X-ray, colourblindness, Blindness have issues currentlyZ

		public Pickupable Pickupable;
		public int BaseBlurryVision = 0;

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		public Color DimLightColour = new Color(255, 255, 255, 1);

		public Reagent EyeIrritant;

		public float EyeIrritantAmount = 0.25f;

		private int _secondsUntilBlink;


		public bool Blinks = true;




		[Header("Blink Duration (seconds)")]
		public float blinkDurationMin = 0.10f;
		public float blinkDurationMax = 0.15f;

		private void OnEnable()
		{
			_secondsUntilBlink = SampleInterval();
		}



		private async UniTaskVoid DoBlink()
		{
			float duration = Random.Range(blinkDurationMin, blinkDurationMax);

			foreach (var Sprite in RelatedPart.RelatedPresentSprites)
			{
				Sprite.baseSpriteHandler.PushClear();
			}


			await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: destroyCancellationToken);
			await UniTask.SwitchToMainThread();
			foreach (var Sprite in RelatedPart.RelatedPresentSprites)
			{
				Sprite.baseSpriteHandler.PushTexture();
			}
		}

		private int SampleInterval()
		{
			return Random.Range(3, 6); // 3–5 seconds, different each time
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
			return true;
		}

		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
			OnBodyID = ShowForPlayer != null ? ShowForPlayer.netId : NetId.Empty;
		}




		public override void ImplantPeriodicUpdate()
		{
			if (RelatedPart.HealthMaster.IsCrit  == false &&  RelatedPart.HealthMaster.IsDead == false && Blinks)
			{
				if (--_secondsUntilBlink <= 0)
				{
					DoBlink().Forget();
					_secondsUntilBlink = SampleInterval();
				}
			}

			//TODO eye Protection
			if (EyeIrritant == null) return;
			if (GameObjectExtensions.OrNull<LivingHealthMasterBase>(RelatedPart?.HealthMaster)?.SurfaceReagents == null) return;

			if (RelatedPart.HealthMaster.SurfaceReagents.TryGetValue(BodyPartType.Head, out var mix) == false)
				//TODO Dynamically work out depending on which body part implanted in,  Instead of assuming it's in the head
			//Simplest way to do it traverse to the highest body part, that it stored in and then work out its category, In the health doll
			{
				 return;
			};


			if (mix.reagents.Contains(EyeIrritant) == false) return;
			float AmountOnHead = mix[EyeIrritant];

			if (AmountOnHead > EyeIrritantAmount)
			{
				if (RelatedPart.ClothingArmors.Count > 0) return;
				var RegisterPlayer = RelatedPart.HealthMaster.GetComponent<RegisterPlayer>();
				if (RegisterPlayer.IsSlippingServer == false)
				{
					RegisterPlayer.ServerStun(AmountOnHead * 5f, true, false);
				}
			}
		}


		public override void Awake()
		{
			base.Awake();
			Pickupable = this.GetComponent<Pickupable>();
			RelatedPart.ModifierChange += UpdateBlurryEye;
			UpdateBlurryEye();
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			base.OnAddedToBody(livingHealth);
			livingHealth.playerScript.DimPlayerLightController.lightColor = DimLightColour;
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			base.OnRemovedFromBody(livingHealth);
			livingHealth.playerScript.DimPlayerLightController.ResetToDefault();
		}

		public void UpdateBlurryEye()
		{
			int Calculated = 0;
			if (RelatedPart.TotalModified < 0.95f)
			{
				Calculated = Mathf.RoundToInt(30 * (1 - (RelatedPart.TotalModified / 0.95f)));
			}

			Calculated = Calculated + BaseBlurryVision;
			SyncBadEyesight(0, Calculated);
		}

		[NaughtyAttributes.Button()]
		public void GiveSite()
		{
			SyncPreventBlindness(false, true);
		}


		[NaughtyAttributes.Button()]
		public void MakeBlind()
		{
			SyncPreventBlindness(false, false);
		}


		#region Synchronise

		private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

		public uint OnPlayerID => OnBodyID;

		[SyncVar(hook = nameof(SyncPreventBlindness))]
		public bool PreventsBlindness = true; //TODO change to multi-interest bool, Is good enough for now, For multiple eyes
		private bool DefaultPreventsBlindness_ = false;

		[SyncVar(hook = nameof(SyncBadEyesight))]
		public int BadEyesight = 0;
		private int DefaultBadEyesight = 0;

		public MultiInterestFloat BadEyesightRecord = new MultiInterestFloat(0, InSetFloatBehaviour: MultiInterestFloat.FloatBehaviour.PickTop);

		[SyncVar(hook = nameof(SyncColourBlindMode))]
		public ColourBlindMode CurrentColourblindness = ColourBlindMode.None;
		private ColourBlindMode DefaultColourblindness = ColourBlindMode.None;

		[SyncVar(hook = nameof(SyncXrayState))]
		public bool HasXray = false;
		private bool DefaultHasXray = false;


		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		public void ApplyDefaultOrCurrentValues(bool Default)
		{
			ApplyChangesBlindness(Default ? DefaultPreventsBlindness_ : PreventsBlindness);
			ApplyChangesBlurryVision(Default ? DefaultBadEyesight : BadEyesight);
			ApplyChangesColourBlindMode(Default ? DefaultColourblindness : CurrentColourblindness);
			ApplyChangesXrayState(Default ? DefaultHasXray : HasXray);
		}


		public void SyncPreventBlindness(bool oldValue, bool newState)
		{
			PreventsBlindness = newState;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesBlindness(PreventsBlindness);
			}
		}

		public void ApplyChangesBlindness(bool SetValue)
		{

			if (SetValue)
			{
				Camera.main.GetComponent<CameraEffectControlScript>().Blindness.RecordPosition(this, !SetValue);
			}
			else
			{
				Camera.main.GetComponent<CameraEffectControlScript>().Blindness.RemovePosition(this);
			}
		}


		public void SyncBadEyesight(int oldValue, int newState)
		{
			BadEyesight = newState;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesBlurryVision((int) BadEyesight);
			}
		}

		public void ApplyChangesBlurryVision(int BlurryStrength)
		{
			Camera.main.GetComponent<CameraEffectControlScript>().blurryVisionEffect
				.SetBlurStrength((int) BlurryStrength);
		}


		public void SyncColourBlindMode(ColourBlindMode NotSetValueServer, ColourBlindMode newState)
		{
			CurrentColourblindness = newState;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesColourBlindMode(CurrentColourblindness);
			}
		}

		public void ApplyChangesColourBlindMode(ColourBlindMode newState)
		{
			Camera.main.GetComponent<CameraEffectControlScript>().colourblindEmulationEffect
				.SetColourMode(newState);
		}

		public void SyncXrayState(bool old, bool newState)
		{
			HasXray = newState;
			if (Preimplemented.IsOnLocalPlayer)
			{
				ApplyChangesXrayState(HasXray);
			}
		}

		public void ApplyChangesXrayState(bool newState)
		{
			if (newState)
			{
				CameraEffectControlScript.Instance.Xray.RecordPosition(this.gameObject, true);
			}
			else
			{
				CameraEffectControlScript.Instance.Xray.RemovePosition(this.gameObject);
			}
		}

		#endregion
	}
}