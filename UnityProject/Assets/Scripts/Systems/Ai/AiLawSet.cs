using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Ai
{
	[CreateAssetMenu(fileName = "AiLawSet", menuName = "ScriptableObjects/Ai/AiLawSet")]
	public class AiLawSet : ScriptableObject
	{
		[SerializeField]
		private string lawSetName;
		public string LawSetName => lawSetName;

		[SerializeField]
		private List<AiLawItem> laws = new List<AiLawItem>();
		public List<AiLawItem> Laws => laws;
	}

	[Serializable]
	public class AiLawItem
	{
		[Tooltip("Dont include beginning numbers just law text e.g 'kill all humans' not '1. kill all humans'")]
		[TextArea(5,5)]
		public string Law;
		public AiPlayer.LawOrder LawOrder = AiPlayer.LawOrder.Core;
	}
}
