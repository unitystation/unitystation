using System.Collections;
using System.Collections.Generic;
using AdminCommands;
using UnityEngine;
using Mirror;
using CustomInspectors;
using Systems.Clearance;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;

namespace Objects.Wallmounts
{
	public class GeneralSwitch : ImnterfaceMultitoolGUI, ISubscriptionController, ICheckedInteractable<HandApply>,
		IMultitoolMasterable, IRightClickable
	{
		private SpriteRenderer spriteRenderer;
		public Sprite greenSprite;
		public Sprite offSprite;
		public Sprite redSprite;

		public List<GeneralSwitchController> generalSwitchControllers = new List<GeneralSwitchController>();

		private bool buttonCoolDown = false;
		private ClearanceRestricted clearanceRestricted;

		private APCPoweredDevice APCPoweredDevice;

		private void Start()
		{
			//This is needed because you can no longer apply shutterSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			clearanceRestricted = GetComponent<ClearanceRestricted>();
			APCPoweredDevice = GetComponent<APCPoweredDevice>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			//this validation is only done client side for their convenience - they can't
			//press button while it's animating.
			if (side == NetworkSide.Client)
			{
				if (buttonCoolDown) return false;
				buttonCoolDown = true;
				StartCoroutine(CoolDown());
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{

			if (clearanceRestricted.HasClearance(interaction.Performer) == false)
			{
				RpcPlayButtonAnim(false);
				return;
			}

			RunDoorController();
		}

		public void RunDoorController()
		{
			if (APCPoweredDevice != null)
			{
				if (APCPoweredDevice.IsOn(PowerState.On) == false) return;
			}

			RpcPlayButtonAnim(true);

			for (int i = 0; i < generalSwitchControllers.Count; i++)
			{
				if (generalSwitchControllers[i] == null) continue;

				//Trigger Event
				generalSwitchControllers[i].SwitchPressedDoAction.Invoke();
			}
		}

		//Stops spamming from players
		IEnumerator CoolDown()
		{
			yield return WaitFor.Seconds(1.2f);
			buttonCoolDown = false;
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

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.GeneralSwitch;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		#region Editor

		void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;

			//Highlighting all controlled doors with red lines and spheres
			Gizmos.color = new Color(1, 0.5f, 0, 1);
			for (int i = 0; i < generalSwitchControllers.Count; i++)
			{
				var generalSwitchController = generalSwitchControllers[i];
				if (generalSwitchController == null) continue;
				Gizmos.DrawLine(sprite.transform.position, generalSwitchController.transform.position);
				Gizmos.DrawSphere(generalSwitchController.transform.position, 0.25f);
			}
		}

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var generalSwitchController = potentialObject.GetComponent<GeneralSwitchController>();
				if (generalSwitchController == null) continue;
				AddDoorControllerFromScene(generalSwitchController);
				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		private void AddDoorControllerFromScene(GeneralSwitchController generalSwitchController)
		{
			if (generalSwitchControllers.Contains(generalSwitchController))
			{
				generalSwitchControllers.Remove(generalSwitchController);
			}
			else
			{
				generalSwitchControllers.Add(generalSwitchController);
			}
		}

		#endregion

		public RightClickableResult GenerateRightClickOptions()
		{
			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) ||
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
