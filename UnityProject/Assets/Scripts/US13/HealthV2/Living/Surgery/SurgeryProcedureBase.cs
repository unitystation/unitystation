using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.HealthV2.Living.BodyParts;
using BodyPart = US13.HealthV2.Living.CirculatorySystem.BodyPart;

namespace US13.HealthV2.Living.Surgery
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