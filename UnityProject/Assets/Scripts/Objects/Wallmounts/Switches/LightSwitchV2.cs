using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using Systems.Electricity;
using Systems.Interaction;
using CustomInspectors;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Shared.Systems.ObjectConnection;
using Systems.Ai;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine.Events;
using Random = UnityEngine.Random;


namespace Objects.Lighting
{
	public class LightSwitchV2 : ImnterfaceMultitoolGUI, ISubscriptionController,
		ICheckedInteractable<HandApply>, IAPCPowerable, IMultitoolMasterable, ICheckedInteractable<AiActivate>, IHoverTooltip
	{
		public List<LightSource> listOfLights;
		public UnityEvent OnButtonPressed = new UnityEvent();

		[SyncVar(hook = nameof(SyncState))]
		public bool isOn = true;

		[SerializeField]
		private float coolDownTime = 2f;

		private bool isInCoolDown;

		[SerializeField]
		private Sprite[] sprites = null;

		[SerializeField]
		private SpriteRenderer spriteRenderer = null;

		private PowerState powerState = PowerState.On;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = false;

		#region Lifecycle

		private void Awake()
		{
			foreach (var lightSource in listOfLights)
			{
				if (lightSource != null)
				{
					lightSource.SubscribeToSwitchEvent(this);
				}
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncState(isOn, isOn);
		}

		#endregion

		private void SyncState(bool oldState, bool newState)
		{
			isOn = newState;
			spriteRenderer.sprite = isOn ? sprites[0] : sprites[1];
		}

		[Server]
		public void ServerChangeState(bool newState, bool invokeEvent = true)
		{
			isOn = newState;
			if (invokeEvent == false) return;
			StartCoroutine(SlowInvoke());
		}

		private IEnumerator SlowInvoke()
		{
			foreach (var thingToInvoke in listOfLights)
			{
				yield return WaitFor.Seconds(Random.Range(0.09f, 0.5f));
				thingToInvoke.Trigger(isOn);
			}
		}

		#region ICheckedInteractable<HandApply>

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (isInCoolDown)
			{
				Chat.AddExamineMsg(interaction.Performer, "You can't use the switch yet.");
				return;
			}
			TryInteraction();
			if (powerState == PowerState.Off || powerState == PowerState.LowVoltage)
			{
				Chat.AddExamineMsg(interaction.Performer, "You flip the switch... But nothing happens.");
				return;
			}
			OnButtonPressed?.Invoke();
			if (interaction.IsAltClick)
			{
				Chat.AddExamineMsg(interaction.Performer, $"You quietly flip the switch back {IsOnOffString()}.");
			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} flips the switch back {IsOnOffString()}.");
			}
			_ = SwitchCoolDown();
		}

		#endregion

		private void TryInteraction()
		{
			if (powerState == PowerState.Off || powerState == PowerState.LowVoltage) return;
			ServerChangeState(!isOn);
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			if (isInCoolDown) return false;

			//Trigger client cooldown only, or else it will break for local host
			if (CustomNetworkManager.IsServer == false)
			{
				_ = SwitchCoolDown();
			}

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//Start server cooldown
			_ = SwitchCoolDown();
			TryInteraction();
		}

		#endregion

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			if (isServer == false) return;
			switch (state)
			{
				case PowerState.OverVoltage:
				case PowerState.On:
					ServerChangeState(true, invokeEvent: false);
					powerState = state;
					break;
				case PowerState.LowVoltage:
				default:
					ServerChangeState(false, invokeEvent: false);
					powerState = state;
					break;
			}
		}

		#endregion

		private async UniTask SwitchCoolDown()
		{
			isInCoolDown = true;
			await UniTask.WaitForSeconds(coolDownTime); // unitask has zero allocations compared to IEnumerators.
			isInCoolDown = false;
		}

		#region Multitool Interaction

		[SerializeField]
		private MultitoolConnectionType conType = MultitoolConnectionType.LightSwitch;
		public MultitoolConnectionType ConType => conType;

		public bool MultiMaster => true; //TODO
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion

		#region Editor

		public IEnumerable<GameObject> SubscribeToController(IEnumerable<GameObject> potentialObjects)
		{
			var approvedObjects = new List<GameObject>();

			foreach (var potentialObject in potentialObjects)
			{
				var lightSource = potentialObject.GetComponent<LightSource>();
				if (lightSource == null) continue;
				AddLightSourceFromScene(lightSource);
				approvedObjects.Add(potentialObject);
			}

			return approvedObjects;
		}

		private void AddLightSourceFromScene(LightSource lightSource)
		{
			if (listOfLights.Contains(lightSource))
			{
				listOfLights.Remove(lightSource);
				lightSource.relatedLightSwitch = null;
			}
			else
			{
				listOfLights.Add(lightSource);
				lightSource.relatedLightSwitch = this;
			}
		}

		#endregion

		#region Hover ToolTips

		public string HoverTip()
		{
			return $"The switch seems to be {IsOnOffString()}.";
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			var interactions = new List<TextColor>
			{
				new TextColor
				{
					Text = $"Click to Flip the switch {IsOnOffString()}.",
					Color = IntentColors.Help
				},
				new TextColor
				{
					Text = $"Alt+Click to quietly Flip the switch {IsOnOffString()}.",
					Color = IntentColors.Help
				},
			};

			if (LocalPlayerHasMultiTool())
			{
				interactions.Add(new TextColor
				{
					Text = "Use the Multitool to rewire the switch.",
					Color = IntentColors.Help
				});
			}

			return interactions;
		}

		private bool LocalPlayerHasMultiTool()
		{
			if (PlayerManager.LocalPlayerScript == null) return false;
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage == null) return false;
			foreach (var slot in PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots())
			{
				if (slot.IsEmpty) continue;
				if (slot.ItemAttributes.GetTraits().Contains(CommonTraits.Instance.Multitool)) return true;
			}

			return false;
		}
		#endregion

		private string IsOnOffString()
		{
			return isOn ? "on" : "off";
		}
	}
}
