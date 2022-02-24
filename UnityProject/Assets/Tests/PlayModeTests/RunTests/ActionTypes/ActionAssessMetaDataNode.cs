using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

// public class AssessMetaDataNode : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowAssessMetaDataNode => SpecifiedAction == ActionType.AssessMetaDataNode;

	[AllowNesting] [ShowIf(nameof(ShowAssessMetaDataNode))] public AssessMetaDataNode DataAssessMetaDataNode;



	[System.Serializable]
	public class AssessMetaDataNode
	{
		public Vector3 WorldPosition;

		public string MatrixName;

		public ClassVariableRead Parameters = new ClassVariableRead();


		public string CustomFailedText;


		public bool Initiate(TestRunSO TestRunSO)
		{
			MatrixInfo _Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, WorldPosition);
			var _MetaDataNode = _Magix.MetaDataLayer.Get(WorldPosition.ToLocal(_Magix).RoundToInt());

			if (Parameters.SatisfiesConditions(_MetaDataNode.GetType(), _MetaDataNode, out var ReportString))
			{
				return true;
			}
			else
			{
				TestRunSO.Report.AppendLine(CustomFailedText);
				TestRunSO.Report.AppendLine($"The Meta data node no did not meet the requirements of the parameters");
				TestRunSO.Report.AppendLine(ReportString);
				return false;
			}

			return true;
		}
	}

	public bool InitiateAssessMetaDataNode(TestRunSO TestRunSO)
	{
		return DataAssessMetaDataNode.Initiate(TestRunSO);
	}

}
