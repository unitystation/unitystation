using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Cooldowns;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Implants.Organs;
using US13.Items.Traits;
using US13.Mobs.BrainAI.States.SimpleBot;
using US13.Player;
using US13.UI.Systems.MainHUD.UI_Bottom;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;

namespace US13.HealthV2.Living
{
	public class EmaggableMob : MonoBehaviour, IHoverTooltip, ICheckedInteractable<PositionalHandApply>, ICooldown
	{
		public float DefaultTime => 0.5f;
		private bool _canBeEmagged = false;
		private Brain _connectedBrain = null;

		public void SetEmaggableState(bool canBeEmagged, Brain connectedBrain)
		{
			_canBeEmagged = canBeEmagged;
			_connectedBrain = connectedBrain;
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (_canBeEmagged == false || _connectedBrain == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent != Intent.Help) return false;
			if (interaction.UsedObject == null) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Emag) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (Cooldowns.TryStart(interaction, this, side: NetworkSide.Server) == false) return;

			if (Vector3.Distance(interaction.Performer.gameObject.AssumedWorldPosServer(), _connectedBrain.gameObject.AssumedWorldPosServer()) > 2f) return;
			if (_connectedBrain.gameObject.TryGetComponent<ICanBeEmaggedMob>(out var emaggableMob))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You successfully sabotage the {gameObject.ExpensiveName()}");
				emaggableMob.EmagMob();
			}
		}

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
			var result = new List<TextColor>();

			var hands = PlayerManager.LocalPlayerScript?.Equipment?.ItemStorage?.GetActiveHandSlot();
			if (_canBeEmagged && hands != null && hands.ItemAttributes != null && hands.ItemAttributes.GetTraits().Contains(CommonTraits.Instance.Emag) == false)
			{
				result.Add(new TextColor() {Text = "Left click while holding an EMAG to sabotage this bot.", Color = Color.red});
			}
			return result;
		}
	}
}