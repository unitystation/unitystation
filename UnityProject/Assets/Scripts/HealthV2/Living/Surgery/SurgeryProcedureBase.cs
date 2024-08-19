using System.Collections;
using System.Collections.Generic;
using HealthV2.Living.Surgery;
using UnityEngine;

namespace HealthV2
{
	[CreateAssetMenu(fileName = "SurgeryProcedureBase", menuName = "ScriptableObjects/Surgery/SurgeryProcedureBase")]
	public class SurgeryProcedureBase : ScriptableObject
	{
		public string ProcedureName;
		public SpriteDataSO ProcedureSprite;
		public List<SurgeryStep> SurgerySteps = new List<SurgeryStep>();

		public virtual void FinnishSurgeryProcedure(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure PresentProcedure)
		{
		}

		public virtual void UnsuccessfulStep(BodyPart OnBodyPart, HandApply interaction,
			PresentProcedure PresentProcedure)
		{
		}
	}
}