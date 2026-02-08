using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EditorButInAssetsAssembly;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Items.Cards;
using US13.Managers;
using US13.Messages.Client.Admin;
using US13.Objects.Doors;
using US13.Objects.Engineering;
using US13.Systems.Clearance;
using US13.Systems.Inventory;
using US13.UI.Core.RightClick;
using Util;
using Util.Independent.FluentRichText;

namespace US13.Objects.Wallmounts.Switches
{
	/// <summary>
	/// Allows object to function as a door switch - opening / closing door when clicked.
	/// </summary>
	[ExecuteInEditMode]
	public class DoorSwitch : ImnterfaceMultitoolGUI, ISubscriptionController, ICheckedInteractable<HandApply>,
		IMultitoolMasterable,
		IServerSpawn, ICheckedInteractable<AiActivate>, IRightClickable
	{
		private SpriteRenderer spriteRenderer;
		public Sprite greenSprite;
		public Sprite offSprite;
		public Sprite redSprite;

		[SerializeField] [Tooltip("List of doors that this switch can control")]

		private List<DoorMasterController> NewdoorControllers = new List<DoorMasterController>();
		public int NewDoorCount => NewdoorControllers.Count;

		private bool buttonCoolDown = false;
		private ClearanceRestricted clearanceRestricted;

		public APCPoweredDevice thisAPCPoweredDevice { get; private set; }

		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = false;
		public void OnSpawnServer(SpawnInfo info)
		{
		}

		private void Start()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			clearanceRestricted = GetComponent<ClearanceRestricted>();
			thisAPCPoweredDevice = GetComponent<APCPoweredDevice>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (TestCoolDown() == false) return;

			var storage = interaction.Performer.OrNull()?.GetComponent<DynamicItemStorage>();

			if (storage != null)
			{
				var emag = Emag.GetEmagInDynamicItemStorage(storage);
				if (emag != null)
				{
					if (emag.UseCharge(interaction))
					{
						RunDoorController(interaction);
						RpcPlayButtonAnim(false);
						return;
					}
				}
			}

			if (clearanceRestricted.HasClearance(interaction.Performer) == false)
			{
				RpcPlayButtonAnim(false);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"The {gameObject.ExpensiveName()} makes a loud buzz as it denies {interaction.PerformerPlayerScript.visibleName}'s clearance.");
				return;
			}
			RunDoorController(interaction);
		}

		public void RunDoorController(HandApply interaction = null)
		{
			if (NewdoorControllers.Count == 0)
			{
				if (interaction != null) Chat.AddExamineMsg(interaction.Performer, "There doesn't seem to be anything connected to this button.");
				return;
			}

			if (thisAPCPoweredDevice != null)
			{
				if (APCPoweredDevice.IsOn(thisAPCPoweredDevice.State) == false)
				{
					if (interaction != null) Chat.AddExamineMsg(interaction.Performer, "There doesn't seem to be power connected to this button.".Color(Color.red));
					return;
				}
			}

			RpcPlayButtonAnim(true);

			if (interaction != null)
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"{interaction.PerformerPlayerScript.visibleName} interacts with the {gameObject.ExpensiveName()}, " +
					$"and a small chirp can be heard as it approves {interaction.PerformerPlayerScript.characterSettings.TheirPronoun(interaction.PerformerPlayerScript)} clearance.");
			}

			foreach (var door in NewdoorControllers)
			{
				// Door doesn't exist anymore - shuttle crash, admin smash, etc.
				if (door == null) continue;

				if (door.IsClosed)
				{
					door.PulseTryOpen(bypassSoftware: true);
				}
				else
				{
					door.PulseTryClose(bypassSoftware: true);
				}
			}
		}

		public void OpenDoors()
		{
			foreach (var door in NewdoorControllers)
			{
				// Door doesn't exist anymore - shuttle crash, admin smash, etc.
				if (door == null) continue;

				if (door.IsClosed)
				{
					door.PulseTryOpen(bypassSoftware: true);
				}
			}
		}

		public void CloseDoors()
		{
			foreach (var door in NewdoorControllers)
			{
				// Door doesn't exist anymore - shuttle crash, admin smash, etc.
				if (door == null) continue;

				if (door.IsClosed == false)
				{
					door.PulseTryClose(bypassSoftware: true);
				}
			}
		}

		//Stops spamming from players
		IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(1.2f);
			buttonCoolDown = false;
		}

		public bool TestCoolDown()
		{
			if (buttonCoolDown)
				return false;
			buttonCoolDown = true;
			StartCoroutine(CoolDown());

			return true;
		}


		[ClientRpc]
		public void RpcPlayButtonAnim(bool status)
		{
			StartCoroutine(ButtonFlashAnim(status));
		}

		IEnumerator ButtonFlashAnim(bool status)
		{
			if (spriteRenderer == null)
			{
				spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			}

			for (int i = 0; i < 6; i++)
			{
				if (status)
				{
					if (spriteRenderer.sprite == greenSprite)
					{
						spriteRenderer.sprite = offSprite;
					}
					else
					{
						spriteRenderer.sprite = greenSprite;
					}

					yield return WaitFor.Seconds(0.2f);
				}
				else
				{
					if (spriteRenderer.sprite == redSprite)
					{
						spriteRenderer.sprite = offSprite;
					}
					else
					{
						spriteRenderer.sprite = redSprite;
					}

					yield return WaitFor.Seconds(0.1f);
				}
			}

			spriteRenderer.sprite = greenSprite;
		}

		#region Editor

		private void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;

			//Highlighting all controlled doors with red lines and spheres
			for (int i = 0; i < NewdoorControllers.Count; i++)
			{
				var doorController = NewdoorControllers[i];
				if (doorController == null) continue;
				Gizmos.DrawLine(sprite.transform.position, doorController.transform.position);
				Gizmos.DrawSphere(doorController.transform.position, 0.25f);
			}
		}

		private void OnDrawGizmos()
		{
			if ((NewdoorControllers.Count == 0 || NewdoorControllers.Any(controller => controller == null)))
			{
				Gizmos.DrawIcon(transform.position, "noDoor");
			}
		}

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var doorController = potentialObject.GetComponent<DoorMasterController>();
				if (doorController != null)
				{
					NewAddDoorControllerFromScene(doorController);
				}

				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		public void NewAddDoorControllerFromScene(DoorMasterController doorController)
		{
			if (NewdoorControllers.Contains(doorController))
			{
				NewdoorControllers.Remove(doorController);
			}
			else
			{
				NewdoorControllers.Add(doorController);
			}
		}

		#endregion

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			RunDoorController();
		}

		#endregion

		#region Multitool Interaction

		[SerializeField] private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true; //TODO
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		public RightClickableResult GenerateRightClickOptions()
		{
			if (PlayerList.HasTAGClient(TAG.ADMIN_PRESS_BUTTON) == false ||
			    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold) == false)
			{
				return null;
			}

			return RightClickableResult.Create()
				.AddAdminElement("Activate", AdminPressButton);
		}

		private void AdminPressButton()
		{
			AdminCommandsManager.Instance.CmdActivateButton(gameObject);
		}
	}
}