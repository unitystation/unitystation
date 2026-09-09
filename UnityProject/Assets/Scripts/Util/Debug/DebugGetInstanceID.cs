using Logs;
using UnityEngine;

namespace Util.Debug
{
	public class DebugGetInstanceID : MonoBehaviour
	{

		public string InstanceID = "";

		[NaughtyAttributes.Button]
		public void getIt()
		{
			Loggy.Error(this.gameObject.GetInstanceID().ToString());
			InstanceID = this.gameObject.GetInstanceID().ToString();
		}

	}
}
