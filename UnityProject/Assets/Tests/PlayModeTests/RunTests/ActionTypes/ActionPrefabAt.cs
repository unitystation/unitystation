using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
using Util;

public partial class TestAction
{
	public bool ShowPrefabAt => SpecifiedAction == ActionType.PrefabAt;

	[AllowNesting] [ShowIf(nameof(ShowPrefabAt))]
	public ShowPrefab DataShowPrefab;

	[System.Serializable]
	public class ShowPrefab
	{
		public GameObject Prefab;
		public bool InverseMustNotPresent = false;
		public Vector3 PositionToCheck;
		[Range(1, 100)] public int NumberPresent = 1;
		[Range(0, 100)] public int TheStackAmount = 0;

		public string MatrixName;

		public string CustomFailedText;

		public bool Initiate(TestRunSO TestRunSO)
		{
			if (NumberPresent == 0)
			{
				NumberPresent = 1;
			}


			var Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, PositionToCheck);
			var List = Magix.Matrix.ServerObjects.Get(PositionToCheck.ToLocal(Magix).RoundToInt());

			var OriginalID = Prefab.GetComponent<PrefabTracker>().ForeverID;


			int Present = 0;

			foreach (var Object in List)
			{
				var PrefabTracker = Object.GetComponent<PrefabTracker>();
				if (PrefabTracker != null)
				{
					if (PrefabTracker.ForeverID == OriginalID)
					{
						Present++;
						if (InverseMustNotPresent)
						{
							TestRunSO.Report.AppendLine(CustomFailedText);
							TestRunSO.Report.AppendLine("Prefab is present, it should not be " + PrefabTracker.gameObject);
							return false;
						}

						if (TheStackAmount != 0)
						{
							var Stackable = PrefabTracker.GetComponent<Stackable>();
							if (Stackable == null)
							{
								TestRunSO.Report.AppendLine(CustomFailedText);
								TestRunSO.Report.AppendLine("Stackable Was not present on " + PrefabTracker.gameObject);
								return false;
							}
							else
							{
								if (Stackable.Amount == TheStackAmount)
								{
									return true;
								}
							}
						}
					}
				}
			}


			if (InverseMustNotPresent)
			{
				return true;
			}

			if (NumberPresent == Present)
			{
				return true;
			}
			else
			{
				TestRunSO.Report.AppendLine(CustomFailedText);
				TestRunSO.Report.AppendLine($"There was not the expected number of gameobjects {Prefab.name} Expected " +
				                            NumberPresent + " Actually " + Present);
				return false;
			}
		}
	}

	public bool InitiatePrefabAt(TestRunSO TestRunSO)
	{
		return DataShowPrefab.Initiate(TestRunSO);
	}
}