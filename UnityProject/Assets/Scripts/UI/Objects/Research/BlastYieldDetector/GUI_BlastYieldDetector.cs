using System;
using UI.Core.NetUI;
using UnityEngine;
using Systems.Research.Objects;

namespace UI.Objects.Research
{
	public class GUI_BlastYieldDetector : NetTab
	{
		public BlastYieldDetector blastYieldDetector
		{
			get
			{
				if (!_blastYieldDetector)
				{
					_blastYieldDetector = Provider.GetComponentInChildren<BlastYieldDetector>();
				}

				return _blastYieldDetector;
			}
		}

		private BlastYieldDetector _blastYieldDetector;

		private GUI_BlastYieldDetector clientGUI;
		private Transform clientGUIGraphTransform;

		#region Serializefields
		[SerializeField]
		private NetText_label pointsMax;

		[SerializeField]
		private NetText_label yieldMin;

		[SerializeField]
		private NetText_label yieldMax;

		[SerializeField]
		public EmptyItemList graphContainer;

		[SerializeField]
		private NetText_label blastYieldLabel;

		[SerializeField]
		private NetText_label pointsLabel;

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
		private void Start()
		{
			clientGUI = UIManager.Instance.transform.GetChild(0).GetComponentInChildren<GUI_BlastYieldDetector>();
			clientGUIGraphTransform = clientGUI.graphContainer.transform;
			BlastYieldDetector.blastEvent += UpdateNodes;
			BlastYieldDetector.serverConnEvent += UpdateServerConnData;
			UpdateNodes();
			UpdateServerConnData(blastYieldDetector.researchServer != null);
		}
		#endregion

		public void UpdateNodes()
		{
			if (blastYieldDetector != null)
			{
				if (blastYieldDetector.blastData.Count == 0)
					return;
				graphContainer.SetItems(blastYieldDetector.blastData.Count);

				for(int i = 0;i<blastYieldDetector.blastData.Count;i++)
				{
					if(i < clientGUIGraphTransform.childCount)
						clientGUIGraphTransform.GetChild(i).GetComponent<RectTransform>().anchoredPosition
						= GetNodePosition(blastYieldDetector.blastData.Keys[i],blastYieldDetector.blastData.Values[i]);
				}
			}
		}

		public void UpdateServerConnData(bool connected)
		{
			if (connected)
			{
				pointsMax.MasterSetValue(blastYieldDetector.maxPointsValue.ToString());
				yieldMin.MasterSetValue(blastYieldDetector.researchServer.yieldTargetRangeMinimum.ToString());
				yieldMax.MasterSetValue(blastYieldDetector.researchServer.yieldTargetRangeMaximum.ToString());
			}
			else
			{
				pointsMax.MasterSetValue("No Server!");
				yieldMin.MasterSetValue("No Server!");
				yieldMax.MasterSetValue("No Server!");
			}


		}

		public Vector2 GetNodePosition(float yield, float points)
		{
			//blast yield axis position
			float difference = blastYieldDetector.researchServer.yieldTargetRangeMaximum -
			                   blastYieldDetector.researchServer.yieldTargetRangeMinimum;
			float yieldClamp = Math.Min(yield,blastYieldDetector.researchServer.yieldTargetRangeMaximum);

			float dotPosX =
				yieldClamp * graphContainer.GetComponent<RectTransform>().rect.width / difference;

			//points axis position
			float dotPosY = points * graphContainer.GetComponent<RectTransform>().rect.height
			                / blastYieldDetector.maxPointsValue;

			//position 2d, third axis isn't important
			Vector2 dotPosition = new Vector2(dotPosX, dotPosY);
			return dotPosition;
		}

		private int dataShown = 0;
		public void DataLeft()
		{
			if (blastYieldDetector.blastData.Count == 0) return;
			if (dataShown - 1 < 0)
			{
				dataShown = blastYieldDetector.blastData.Count-1;
			}
			else
			{
				dataShown--;
			}

			UpdateDataDisplay();
		}

		public void DataRight()
		{
			if (blastYieldDetector.blastData.Count == 0) return;
			if (dataShown + 1 > blastYieldDetector.blastData.Count-1)
			{
				dataShown = 0;
			}
			else
			{
				dataShown++;
			}

			UpdateDataDisplay();
		}

		public void SetCurrentShownData(int pos)
		{
			dataShown = pos;
			UpdateDataDisplay();
		}

		/// <summary>
		/// Moves highlight lines to current node, and updates labels
		/// </summary>
		public void UpdateDataDisplay()
		{
			float yield = blastYieldDetector.blastData.Keys[dataShown];
			float points = blastYieldDetector.blastData.Values[dataShown];
			Vector2 dataShownPos = GetNodePosition(yield, points);

			blastYieldLabel.MasterSetValue(yield.ToString());
			pointsLabel.MasterSetValue(points.ToString());

			Vector3 yieldNewY = yieldNodeHighlight.anchoredPosition;
			yieldNewY.y = dataShownPos.y + rectOffset;
			clientGUI.yieldNodeHighlight.anchoredPosition = yieldNewY;

			Vector3 pointNewX = pointNodeHighlight.anchoredPosition;
			pointNewX.x = dataShownPos.x + rectOffset;
			clientGUI.pointNodeHighlight.anchoredPosition = pointNewX;
		}

		public void OnDestroy()
		{
			BlastYieldDetector.blastEvent -= UpdateNodes;
			BlastYieldDetector.serverConnEvent -= UpdateServerConnData;
		}
	}
}