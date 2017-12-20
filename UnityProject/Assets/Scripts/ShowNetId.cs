using UnityEngine.Networking;

public class ShowNetId : NetworkBehaviour
{
	public uint netId2;

	// Update is called once per frame
	private void Update()
	{
		netId2 = netId.Value;
	}
}