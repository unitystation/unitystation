using System;
using Logs;
using Systems.Explosions;
using Objects.Construction;
using UnityEngine;
using Systems.Electricity.NodeModules;

namespace Objects.Engineering
{
	public class TeslaCoil : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable, IOnLightningHit
	{
		[SerializeField]
		private TeslaCoilState currentState = TeslaCoilState.Power;
		public TeslaCoilState CurrentState => currentState;

		private ModuleSupplyingDevice moduleSupplyingDevice;
		private WrenchSecurable wrenchSecurable;
		private Integrity integrity;

		public bool IsWrenched => wrenchSecurable.IsAnchored;

		private bool hitRecently;
		private int hitTimer;

		private float generatedWatts = 0f;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		[SerializeField]
		[Tooltip("Whether this tesla coil/ grounding rod should start wrenched")]
		private bool startSetUp;

		[SerializeField]
		[Tooltip("lightningHitWatts is how many watts this device will supply")]
		private float lightningHitWatts = 10f;

		#region LifeCycle

		private void Awake()
		{
			if (currentState != TeslaCoilState.Grounding)
			{
				moduleSupplyingDevice = GetComponent<ModuleSupplyingDevice>();
			}
			wrenchSecurable = GetComponent<WrenchSecurable>();
			integrity = GetComponent<Integrity>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(TeslaCoilUpdate, 1f);
			wrenchSecurable.OnAnchoredChange.AddListener(OnWrench);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TeslaCoilUpdate);
			wrenchSecurable.OnAnchoredChange.RemoveListener(OnWrench);
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (startSetUp)
			{
				wrenchSecurable.ServerSetPushable(false);
				UpdateSprite();
			}
		}

		#endregion

		private void TeslaCoilUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(CurrentState == TeslaCoilState.Grounding || IsWrenched == false) return;

			hitTimer--;

			if (hitTimer <= 0)
			{
				hitTimer = 0;
				hitRecently = false;

				if (generatedWatts != 0)
				{
					moduleSupplyingDevice.TurnOffSupply();
				}
			}
			else
			{
				if(generatedWatts == 0)
				{
					moduleSupplyingDevice.TurnOnSupply();
				}

				hitRecently = true;
			}

			if (CurrentState == TeslaCoilState.Power)
			{
				generatedWatts = hitRecently ? lightningHitWatts : 0;

				moduleSupplyingDevice.ProducingWatts = generatedWatts;

				if (hitRecently == false)
				{
					spriteHandler.ChangeSprite(1);
				}
			}
			else
			{
				//TODO generate research points
				generatedWatts = 0;

				if (hitRecently == false)
				{
					spriteHandler.ChangeSprite(3);
				}
			}
		}

		//On hit set timer to 5 seconds, generate 5 seconds worth of power then stop unless hit again
		public void OnLightningHit(float duration, float damage)
		{
			if (IsWrenched == false)
			{
				//We do damage here as the lightning check in TeslaEnergyBall ignores these machines as
				//they are lightning proof, but we dont want that when they're unwrenched
				integrity.ApplyDamage(damage, AttackType.Magic, DamageType.Burn, explodeOnDestroy: true);
				return;
			}

			hitTimer = 5;

			if (CurrentState == TeslaCoilState.Grounding && spriteHandler.CurrentSpriteIndex != 2)
			{
				spriteHandler.ChangeSprite(2);
				return;
			}

			if (spriteHandler.CurrentSpriteIndex < 2)
			{
				spriteHandler.ChangeSprite(2);
			}
			else if(spriteHandler.CurrentSpriteIndex != 2 && spriteHandler.CurrentSpriteIndex < 5)
			{
				spriteHandler.ChangeSprite(5);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (CurrentState == TeslaCoilState.Grounding) return;

			TryScrewdriver(interaction);
		}

		private void TryScrewdriver(HandApply interaction)
		{
			if (IsWrenched && CurrentState == TeslaCoilState.Power)
			{
				currentState = TeslaCoilState.Research;
				UpdateSprite();
				Chat.AddActionMsgToChat(interaction.Performer, "You switch the tesla coil into research mode",
					$"{interaction.Performer.ExpensiveName()} switches the tesla coil into research mode");
			}
			else if (IsWrenched)
			{
				currentState = TeslaCoilState.Power;
				UpdateSprite();
				spriteHandler.AnimateOnce(1);
				Chat.AddActionMsgToChat(interaction.Performer, "You switch the tesla coil into power mode",
					$"{interaction.Performer.ExpensiveName()} switches the tesla coil into power mode");
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The tesla coil needs to be wrenched");
			}
		}

		private void OnWrench()
		{
			UpdateSprite();
		}

		//Called by wrenchSecurable
		private void UpdateSprite()
		{
			switch (CurrentState)
			{
				case TeslaCoilState.Power:
					spriteHandler.ChangeSprite(IsWrenched ? 1 : 0);
					break;
				case TeslaCoilState.Research:
					spriteHandler.ChangeSprite(IsWrenched ? 4 : 3);
					break;
				case TeslaCoilState.Grounding:
					spriteHandler.ChangeSprite(IsWrenched ? 1 : 0);
					break;
				default:
					Loggy.LogError("Tried to wrench Tesla Coil, but switch case was out of bounds", Category.Machines);
					break;
			}
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (CurrentState != TeslaCoilState.Power) return null;

			return $"Generating {generatedWatts} watts of energy";
		}

		public enum TeslaCoilState
		{
			Power,
			Research,

			//using tesla script for ground rods as well to avoid excess scripts
			Grounding
		}
	}
}