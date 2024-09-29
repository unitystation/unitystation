using UnityEngine;
using Objects.Traps;
using UI.Systems.Tooltips.HoverTooltips;
using System.Collections.Generic;
using Systems.Clearance;

namespace Objects.Logic
{
	[RequireComponent(typeof(BasicClearanceSource))]
	public class LogicClearanceProvider : GenericTriggerOutput, ICheckedInteractable<HandApply>, IGenericTrigger, IHoverTooltip
	{
		private bool state = false;

		[SerializeField] private SpriteHandler spriteHandler = null;
		[SerializeField] private SpriteHandler inputSpriteHandler = null;
		private BasicClearanceSource clearanceSource = null;
		[field: SerializeField] public TriggerType TriggerType { get; protected set; }

		protected override void Awake()
		{
			clearanceSource = GetComponent<BasicClearanceSource>();
			SyncList();
		}

		public void OnTrigger()
		{
			inputSpriteHandler.SetSpriteVariant(1);

			if (TriggerType == TriggerType.Toggle) ToggleState();
			else if (state == false) EnableOutput();
		}

		private void ToggleState()
		{
			if (state == true) DisableOutput();
			else EnableOutput();
		}

		public void OnTriggerEnd()
		{
			inputSpriteHandler.SetSpriteVariant(0);

			if (TriggerType != TriggerType.Active) return;
			DisableOutput();
		}

		public void EnableOutput()
		{
			state = true;
			spriteHandler.SetSpriteVariant(1);
			TriggerOutputWithClearance(clearanceSource);
		}

		private void DisableOutput()
		{
			state = false;
			spriteHandler.SetSpriteVariant(0);
			ReleaseOutput();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side, AllowTelekinesis: false) == false) return false;

			// only allow interactions targeting this
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return false;

			return true;
		}

		//With multiple possible inputs to connect with a multitool, we allow the player to switch between what one they want to connect to.
		public void ServerPerformInteraction(HandApply interaction)
		{
			clearanceSource.ServerSetClearance(ClearanceRestricted.GrabClearance(interaction.Performer)[0].IssuedClearance); //Adds first clearance source to add to set this object to.
			clearanceSource.ServerSetLowPopClearance(ClearanceRestricted.GrabClearance(interaction.Performer)[0].LowPopIssuedClearance); 

			Chat.AddExamineMsgFromServer(interaction.Performer, $"You change the clearance on this device to {clearanceSource.IssuedClearance}");
		}


		#region Tooltips

		public string HoverTip()
		{
			return null;
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
			List<TextColor> interactions = new List<TextColor>();
			TextColor text = new TextColor
			{
				Text = $"The provider currently has clearance of: {clearanceSource.IssuedClearance}",
				Color = IntentColors.Help
			};
			interactions.Add(text);
			
			return interactions;
		}
		#endregion
	}
}
