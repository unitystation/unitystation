﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		public List<SurgeryProcedureBase> SurgeryProcedureBase = new List<SurgeryProcedureBase>();


		public virtual void SuccessfulProcedure(PositionalHandApply interaction, Dissectible.PresentProcedure PresentProcedure)
		{
			//Do you whatever you would like
		}

		public virtual void UnsuccessfulStep(PositionalHandApply interaction, Dissectible.PresentProcedure PresentProcedure)
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
		public int SuccessChance = 100;

		public string StartSelf = "";
		public string StartOther = "";

		public string SuccessSelf = "";
		public string SuccessOther = "";
		public string FailSelf = "";
		public string FailOther = "";

	}

}