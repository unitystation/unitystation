using UnityEngine;
using US13.HealthV2.Living;
using US13.Systems.Clearance;

namespace US13.Items.Implants.Organs
{
	public class BodyPartAccess : BodyPartFunctionality
	{
		public IClearanceSource IClearanceSource;

		public void Awake()
		{
			IClearanceSource = this.GetComponent<IClearanceSource>();
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			var GroupedAccess = livingHealth.GetComponent<GroupedAccess>();
			if (GroupedAccess != null)
			{
				GroupedAccess.RemoveIClearanceSource(IClearanceSource);
			}
		}

		public override void OnAddedToBody(LivingHealthMasterBase livingHealth)
		{
			var GroupedAccess = livingHealth.GetComponent<GroupedAccess>();
			if (GroupedAccess != null)
			{
				GroupedAccess.AddIClearanceSource(IClearanceSource);
			}
		} //Warning only add body parts do not remove body parts in this
	}
}