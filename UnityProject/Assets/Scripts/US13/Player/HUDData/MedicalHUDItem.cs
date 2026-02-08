using US13.Clothing.Eyewear;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Player.HUDData
{
	public class MedicalHUDItem : HUDItemBase
	{
		public override bool IsValidSetup(RegisterPlayer player)
		{
			if (player == null) return false;
			if (player != null && player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player &&
			    (pickupable.ItemSlot is {NamedSlot: NamedSlot.eyes} ||  pickupable.ItemSlot.NamedSlot == null && pickupable.ItemSlot.ItemStorage.GetComponent<BodyPart>() != null )
			   ) // Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
			{
				return true;
			}

			return false;
		}


		public override void ApplyEffects(bool State)
		{
			var HudType = typeof(MedicalHUD);
			if (HUDHandler.Categorys.ContainsKey(HudType))
			{
				var Listy = HUDHandler.Categorys[HudType];
				foreach (var HUD in Listy)
				{
					HUD.SetVisible(State);
				}
			}

			HUDHandler.CategoryEnabled[HudType] = State;
		}
	}
}