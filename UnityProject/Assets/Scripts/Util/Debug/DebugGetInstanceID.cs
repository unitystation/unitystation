using Logs;
using UnityEngine;

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
