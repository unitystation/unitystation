using UnityEngine;
using US13.Shuttles;

namespace US13.Managers.MatrixManager
{
	public class CentralCommandBeacon : MonoBehaviour
	{
		public GuidanceBuoy CentCommGuidanceBuoy;
		private CentComm CentComm;

		void Start()
		{
			CentComm = GameManager.Instance.GetComponent<CentComm>();
			CentComm.CentCommGuidanceBuoy = CentCommGuidanceBuoy;
		}
	}
}