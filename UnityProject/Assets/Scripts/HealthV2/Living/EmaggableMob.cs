using System.Collections.Generic;
using System.Linq;
using HealthV2.Living.PolymorphicSystems;
using Mobs.BrainAI.States.SimpleBot;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace HealthV2.Living
{
	public class EmaggableMob : HealthSystemBase, IHoverTooltip, ICheckedInteractable<PositionalHandApply>, ICooldown
	{
		public float DefaultTime => 0.5f;

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent != Intent.Help) return false;
			if (interaction.TargetObject == interaction.Performer) return false;
			if (interaction.UsedObject == null) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Emag) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (Cooldowns.TryStart(interaction, this, side: NetworkSide.Server) == false) return;

			if (Vector3.Distance(interaction.Performer.gameObject.AssumedWorldPosServer(), Base.gameObject.AssumedWorldPosServer()) > 2f) return;
			if (Base.brain.TryGetComponent<ICanBeEmaggedMob>(out var emaggableMob)) emaggableMob.EmagMob();
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

		public override HealthSystemBase CloneThisSystem()
		{
			return new EmaggableMob();
		}

		public List<TextColor> InteractionsStrings()
		{
			var result = new List<TextColor>();

			var hands = PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetActiveHandSlot();
			if (hands != null && hands.ItemAttributes != null && hands.ItemAttributes.GetTraits().Contains(CommonTraits.Instance.Emag) == false)
			{
				result.Add(new TextColor() {Text = "Left click while holding an EMAG to sabotage this bot.", Color = Color.red});
			}
			return result;
		}
	}
}