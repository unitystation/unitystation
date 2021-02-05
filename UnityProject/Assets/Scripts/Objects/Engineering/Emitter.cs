using System;
using Core.Directionals;
using UnityEngine;
using Weapons;

namespace Objects.Engineering
{
	public class Emitter : MonoBehaviour, ICheckedInteractable<HandApply>, INodeControl
	{
		[SerializeField]
		private GameObject projectilePrefab = default;

		private Directional directional;
		private PushPull pushPull;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private AccessRestrictions accessRestrictions;
		private ElectricalNodeControl electricalNodeControl;
		private ResistanceSourceModule resistanceSourceModule;

		[SerializeField]
		private bool alwaysShoot;

		private bool isWelded;
		private bool isWrenched;
		private bool isOn;
		private bool isLocked;

		private float voltage;

		private void Awake()
		{
			directional = GetComponent<Directional>();
			pushPull = GetComponent<PushPull>();
			registerTile = GetComponent<RegisterTile>();
			accessRestrictions = GetComponent<AccessRestrictions>();
			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			resistanceSourceModule = GetComponent<ResistanceSourceModule>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(EmitterUpdate, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, EmitterUpdate);
		}

		private void EmitterUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(isOn == false && alwaysShoot == false) return;

			if (voltage < 2700)
			{
				spriteHandler.ChangeSprite(2);
				return;
			}

			TogglePower(!isOn);

			ShootEmitter();
		}

		public void ShootEmitter()
		{
			CastProjectileMessage.SendToAll(gameObject, projectilePrefab, directional.CurrentDirection.Vector, default);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.HandApply(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id)) return true;

			if (interaction.HandObject == null) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				TryWrench(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder))
			{
				TryWeld(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
			{
				TryToggleLock(interaction);
			}
			else
			{
				TryToggleOnOff(interaction);
			}
		}

		private void TryToggleLock(HandApply interaction)
		{
			if (accessRestrictions.CheckAccessCard(interaction.HandObject))
			{
				isLocked = !isLocked;

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You {(isLocked ? "lock" : "unlock" )} the emitter",
					$"{interaction.Performer.ExpensiveName()} {(isLocked ? "locks" : "unlocks" )} the emitter");
			}
		}

		private void TryToggleOnOff(HandApply interaction)
		{
			if (isLocked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The emitter needs to be unlocked first");
				return;
			}

			if (isOn)
			{
				TogglePower(false);
			}
			else if (isWelded)
			{
				TogglePower(true);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be wrench and welded down first");
			}
		}

		private void TogglePower(bool newIsOn)
		{
			if (newIsOn)
			{
				isOn = true;
				spriteHandler.ChangeSprite(1);
			}
			else
			{
				isOn = false;
				spriteHandler.ChangeSprite(0);
			}
		}

		private void TryWeld(HandApply interaction)
		{
			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be turned off first");
				return;
			}

			if (isWelded)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to unweld the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to unweld the emitter...",
					"You unweld the emitter from the floor.",
					$"{interaction.Performer.ExpensiveName()} unwelds the emitter from the floor.",
					() =>
					{
						isWelded = false;
						TogglePower(false);
					});
			}
			else
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to weld the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to weld the emitter...",
					"You weld the emitter to the floor.",
					$"{interaction.Performer.ExpensiveName()} welds the emitter to the floor.",
					() => { isWelded = true; });
			}
		}

		private void TryWrench(HandApply interaction)
		{
			if (isWrenched && isWelded)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be unwelded first");
			}
			else if (isWrenched)
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the emitter...",
					"You wrench the emitter off the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the emitter off the floor.",
					() =>
					{
						isWrenched = false;
						directional.LockDirection = false;
						pushPull.ServerSetPushable(true);
						TogglePower(false);
					});
			}
			else
			{
				if (!registerTile.Matrix.MetaTileMap.HasTile(registerTile.WorldPositionServer, LayerType.Base))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be on a base floor");
					return;
				}

				//wrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the emitter...",
					"You wrench the emitter onto the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the emitter onto the floor.",
					() =>
					{
						isWrenched = true;
						directional.LockDirection = true;
						pushPull.ServerSetPushable(false);
					});
			}
		}

		public void PowerNetworkUpdate()
		{
			voltage = electricalNodeControl.GetVoltage();
			//resistanceSourceModule.Resistance = 50f;
			Debug.LogError("voltage: " + voltage);
		}
	}
}
