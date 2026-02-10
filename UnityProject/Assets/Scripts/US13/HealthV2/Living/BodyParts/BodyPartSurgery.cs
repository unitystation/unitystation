using System.Collections.Generic;
using NaughtyAttributes;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living.Surgery;
using US13.Items.Traits;

namespace US13.HealthV2.Living.BodyParts
{
	public partial class BodyPart
	{
		[HorizontalLine]
		public List<SurgeryProcedureBase> SurgeryProcedureBase = new List<SurgeryProcedureBase>();


		public virtual void SuccessfulProcedure(HandApply interaction, PresentProcedure PresentProcedure)
		{
			//Do you whatever you would like
		}

		public virtual void UnsuccessfulStep(HandApply interaction, PresentProcedure PresentProcedure)
		{
			//DO dmg
			//Do you whatever you would like
		}
	}

	[System.Serializable]
	public class SurgeryStep
	{
		public ItemTrait RequiredTrait;
		public float Time; //ues tool
		public int FailChance;

		public string StartSelf = "";
		public string StartOther = "";

		public string SuccessSelf = "";
		public string SuccessOther = "";
		public string FailSelf = "";
		public string FailOther = "";
		public float BleedStacksToAdd = 0;
	}

}