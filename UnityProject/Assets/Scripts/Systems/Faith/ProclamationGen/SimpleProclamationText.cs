using UnityEngine;

namespace Systems.Faith.ProclamationGen
{
	public class SimpleProclamationText : IFaithProclamationTextGenerator
	{
		[SerializeField] private string Proclamation = "I believe";
		[SerializeField] private string Rejection = "I do not believe anymore";

		public string GenerateProclamation()
		{
			return Proclamation;
		}

		public string GenerateRejection()
		{
			return Rejection;
		}
	}
}