using System.Collections;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Research
{
	public class GUI_BlastYieldDetector : NetTab
	{
		public BlastYieldDetector BlastYieldDetector;

		#region Serializefields
		[SerializeField]
		private NetLabel pointsMax;

		[SerializeField]
		private NetLabel yieldMin;

		[SerializeField]
		private NetLabel yieldMax;

		#endregion

		#region Initialization
		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			// Makes sure it connects with the machine
			BlastYieldDetector = Provider.GetComponentInChildren<BlastYieldDetector>();
			// Subscribe to change event from ChemMaster.cs
			BlastYieldDetector.changeEvent += UpdateAll;
			UpdateAll();
			pointsMax.SetValueServer(BlastYieldDetector.maxPointsValue.ToString());
			yieldMin.SetValueServer(BlastYieldDetector.researchServer.yieldTargetRangeMinimum.ToString());
			yieldMax.SetValueServer(BlastYieldDetector.researchServer.yieldTargetRangeMaximum.ToString());
		}
		#endregion

		public void UpdateAll()
		{

		}

		public void OnDestroy()
		{
			BlastYieldDetector.changeEvent -= UpdateAll;
		}
	}
}