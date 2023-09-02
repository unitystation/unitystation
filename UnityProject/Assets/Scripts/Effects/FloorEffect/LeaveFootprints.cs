using Chemistry.Components;
using Effects.FloorEffect;
using Logs;

namespace Objects.Other
{
	/// <summary>
	/// Allows shows to leave footprints when worn.
	/// TODO: Do not make this inherit off FloorHarzard, make a base "FloorTrigger" prefab for this and hazards
	/// </summary>
	///
	public class LeaveFootprints : FloorHazard
	{
		public ReagentContainer reagentContainer;

		public void Update()
		{
			return;
			if (reagentContainer.CurrentReagentMix.Total == 0)
			{
				Loggy.LogError("AAAA");
			}
			Loggy.LogError(reagentContainer.CurrentReagentMix.ToString() + "_" + name);
		}

		public void GiveFootprints(MakesFootPrints print = null, int index = 0)
		{
			if(reagentContainer.CurrentReagentMix.Total > 1f)
			{
				reagentContainer.TransferTo(reagentContainer.CurrentReagentMix.Total * 0.10f, print.spillContents);
			}
		}

		public override void OnPlayerStep(PlayerScript eventData)
		{
			var playerStorage = eventData.gameObject.GetComponent<DynamicItemStorage>();

			if (playerStorage != null)
			{
				foreach (var feetSlot in playerStorage.GetNamedItemSlots(NamedSlot.feet))
				{
					GiveFootprints(feetSlot.ItemObject.gameObject.GetComponent<MakesFootPrints>());
				}
			}

		}

		public override bool WillAffectPlayer(PlayerScript eventData)
		{

			var playerStorage = eventData.gameObject.GetComponent<DynamicItemStorage>();
			if (playerStorage != null)
			{
				foreach (var feetSlot in playerStorage.GetNamedItemSlots(NamedSlot.feet))
				{

					if (feetSlot.ItemObject == null) continue;
					if (feetSlot.ItemObject.gameObject.TryGetComponent<MakesFootPrints>(out var _))
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			return false;
		}
	}
}
