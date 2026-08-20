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
			Loggy.Error(this.gameObject.GetEntityId().ToString());
			InstanceID = this.gameObject.GetEntityId().ToString();
		}

	}
}
