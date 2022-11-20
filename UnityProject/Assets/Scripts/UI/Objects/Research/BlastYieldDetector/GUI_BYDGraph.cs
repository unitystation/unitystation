using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Objects;
using Mirror;

namespace UI.Objects.Research
{
	public class GUI_BYDGraph : NetworkBehaviour
	{
		private const int YAXIS_MAX = 1000; //Up to 1000 Blast yield
		private const int XAXIS_MAX = 10; //Up to last 10 datapoints

		[SerializeField]
		public EmptyItemList graphContainer;

		/// <summary>
		/// Offset to position highlight line UI properly
		/// </summary>
		public float rectOffset;
		[SerializeField]
		private RectTransform yieldNodeHighlight;

		[SerializeField]
		private RectTransform pointNodeHighlight;


		public Vector2 GetNodePosition(float yield, float index)
		{
			float yieldClamp = Math.Min(yield, YAXIS_MAX);

			float dotPosY = yieldClamp * graphContainer.GetComponent<RectTransform>().rect.height / YAXIS_MAX;

			//points axis position
			float dotPosX = index * graphContainer.GetComponent<RectTransform>().rect.width / XAXIS_MAX;

			//position 2d, third axis isn't important
			Vector2 dotPosition = new Vector2(dotPosX, dotPosY);
			return dotPosition;
		}

		[Server]
		public void UpdateDataDisplay(BlastYieldDetector detectorObj)
		{
			RpcUpdateDataDisplay(detectorObj);
		}

		[ClientRpc]
		private void RpcUpdateDataDisplay(BlastYieldDetector blastYieldDetector)
		{
			if (blastYieldDetector == null || blastYieldDetector.BlastYieldData == null)
			{
				graphContainer.Clear();
				return;
			}

			List<float> yields = blastYieldDetector.BlastYieldData.ToList();
			if (yields.Count > XAXIS_MAX)
			{
				yields = yields.GetRange(blastYieldDetector.BlastYieldData.Count - 1 - XAXIS_MAX, XAXIS_MAX); //Obtains last ten datapoints
			}

			graphContainer.SetItems(yields.Count);

			for (int i = 0; i < yields.Count; i++)
			{
				Vector2 dataShownPos = GetNodePosition(yields[i], i);

				if (i < graphContainer.transform.childCount)
				{
					graphContainer.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = dataShownPos;
				}

				if (i != yields.Count - 1) continue;

				Vector3 yieldNewY = yieldNodeHighlight.anchoredPosition;
				yieldNewY.y = dataShownPos.y + rectOffset;
				yieldNodeHighlight.anchoredPosition = yieldNewY;

				Vector3 pointNewX = pointNodeHighlight.anchoredPosition;
				pointNewX.x = dataShownPos.x + rectOffset;
				pointNodeHighlight.anchoredPosition = pointNewX;
			}
		}
	}
}
