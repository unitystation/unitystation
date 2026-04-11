using Logs;
using US13.Clothing.Eyewear;
using US13.HealthV2.Living.BodyParts;
using US13.Systems.ChemistryEffects;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using WebSocketSharp;

namespace US13.Player.HUDData
{
	public class DiagnosticsHUDItem : HUDItemBase
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
			//Power Bar and Power state huds are seperate as machine can have one or the other or both
			var stateHUD = typeof(DiagnosticsHUDPowerState);
			if (HUDHandler.Categorys.ContainsKey(stateHUD))
			{
				var allStateHUDs = HUDHandler.Categorys[stateHUD];
				foreach (var HUD in allStateHUDs)
				{
					HUD.SetVisible(State);
				}
			}
			HUDHandler.CategoryEnabled[stateHUD] = State;

			var barHUD = typeof(DiagnosticsHUDPowerBar);
			if (HUDHandler.Categorys.ContainsKey(barHUD))
			{
				var allBarHUDs = HUDHandler.Categorys[barHUD];
				foreach (var HUD in allBarHUDs)
				{
					HUD.SetVisible(State);
				}
			}
			HUDHandler.CategoryEnabled[barHUD] = State;
		}
	}
}