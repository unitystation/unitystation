using System.Collections;
using UI.Core.NetUI;
using UnityEngine;
using Research;

namespace UI.Objects.Research
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

		[SerializeField]
		private EmptyItemList graphContainer;

		[SerializeField]
		private NetLabel blastYieldLabel;

		[SerializeField]
		private NetLabel pointsLabel;

		/// <summary>
		/// Offset to position highlight line UI properly
		/// </summary>
		public float rectOffset;
		[SerializeField]
		private RectTransform yieldNodeHighlight;

		[SerializeField]
		private RectTransform pointNodeHighlight;

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
			for (;graphContainer.Entries.Length < BlastYieldDetector.blastData.Length;)
			{
				DynamicEntry dot = graphContainer.AddItem();

				//blast yield axis position
				float yield = BlastYieldDetector.blastData[graphContainer.Entries.Length + 1].x;
				float difference = BlastYieldDetector.researchServer.yieldTargetRangeMaximum -
				                   BlastYieldDetector.researchServer.yieldTargetRangeMinimum;
				float dotPosX =
					(yield *graphContainer.GetComponent<RectTransform>().rect.width) / difference;


				//points axis position
				float points = BlastYieldDetector.blastData[graphContainer.Entries.Length + 1].y;
				float dotPosY =
					(points *graphContainer.GetComponent<RectTransform>().rect.height) / BlastYieldDetector.maxPointsValue;

				//position 2d
				Vector3 dotPosition = new Vector3(dotPosX,dotPosY);

				dot.GetComponent<RectTransform>().position = dotPosition;
			}
		}

		private int DataShown;
		public void DataLeft()
		{
			if (BlastYieldDetector.blastData.Length == 0) return;
			if (DataShown - 1 < 0)
			{
				DataShown = BlastYieldDetector.blastData.Length;
			}
			else
			{
				DataShown--;
			}

			UpdateData();
		}

		public void DataRight()
		{
			if (BlastYieldDetector.blastData.Length == 0) return;
			if (DataShown + 1 > BlastYieldDetector.blastData.Length)
			{
				DataShown = 0;
			}
			else
			{
				DataShown++;
			}

			UpdateData();
		}

		public void SetData(int pos)
		{
			DataShown = pos;
			UpdateData();
		}
		public void UpdateData()
		{
			float yield = BlastYieldDetector.blastData[DataShown].x;
			float points = BlastYieldDetector.blastData[DataShown].y;

			blastYieldLabel.Value = yield.ToString();
			pointsLabel.Value = points.ToString();

			Vector3 yieldNewY = yieldNodeHighlight.anchoredPosition;
			yieldNewY.y = points + rectOffset;
			yieldNodeHighlight.SetPositionAndRotation(yieldNewY, Quaternion.Euler(Vector3.zero));

			Vector3 pointNewX = pointNodeHighlight.anchoredPosition;
			pointNewX.x = yield + rectOffset;
			pointNodeHighlight.SetPositionAndRotation(pointNewX, Quaternion.Euler(Vector3.zero));
		}
		public void OnDestroy()
		{
			BlastYieldDetector.changeEvent -= UpdateAll;
		}
	}
}