using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.Player;

namespace US13.Items.Implants.Organs
{
	public class BodyPartRadioAccess : BodyPartFunctionality
	{
		[NaughtyAttributes.EnumFlags]
		public ChatChannel AvailableChannels;

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			var CombinedRadioAccess = livingHealth.GetComponent<CombinedRadioAccess>();
			if (CombinedRadioAccess != null)
			{
				CombinedRadioAccess.RemoveAccess(this);
			}
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			var CombinedRadioAccess = livingHealth.GetComponent<CombinedRadioAccess>();
			if (CombinedRadioAccess != null)
			{
				CombinedRadioAccess.AddAccess(this);
			}
		} //Warning only add body parts do not remove body parts in this
	}
}
