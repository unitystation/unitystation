using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using HealthV2.Living.PolymorphicSystems;
using Mirror;
using Mobs.BrainAI.States.SimpleBot;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace HealthV2.Living
{
	public class EmaggableMob : HealthSystemBase, IHoverTooltip, IRightClickable
	{
		[Command(requiresAuthority = false)]
		public void ServerPerformInteraction(PlayerScript performer)
		{
			if (Vector3.Distance(performer.gameObject.AssumedWorldPosServer(), Base.gameObject.AssumedWorldPosServer()) > 2f) return;
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
				result.Add(new TextColor() {Text = "Right click while holding an EMAG to sabotage this bot.", Color = Color.red});
			}
			return result;
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = new RightClickableResult();
			if (PlayerManager.LocalPlayerScript.Equipment?.ItemStorage == null) return null;

			foreach (var slot in PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetHandSlots())
			{
				if (slot == null || slot.IsEmpty) continue;
				if (slot.ItemAttributes.GetTraits().Contains(CommonTraits.Instance.Emag) == false) continue;
				result.AddElement("Sabotage Bot", () => ServerPerformInteraction(PlayerManager.LocalPlayerScript), Color.red);
			}
			return result;
		}
	}
}