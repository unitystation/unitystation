using System.Collections.Generic;
using UnityEngine;
using Util;

namespace US13.Systems.Faith.ProclamationGen
{
	public class RandomListProclamationText : IFaithProclamationTextGenerator
	{
		[SerializeField] private List<string> Proclamation = new();
		[SerializeField] private List<string> Rejection = new();

		public string GenerateProclamation()
		{
			return Proclamation.PickRandom();
		}

		public string GenerateRejection()
		{
			return Rejection.PickRandom();
		}
	}
}