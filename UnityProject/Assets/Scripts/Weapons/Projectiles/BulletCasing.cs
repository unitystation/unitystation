using UnityEngine;
using UnityEngine.Networking;

public class BulletCasing : NetworkBehaviour
{
	public GameObject spriteObj;

	private void Start()
	{
		if (isServer)
		{
			var netTransform = GetComponent<CustomNetTransform>();

			netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f)));
		}

		var axis = new Vector3(0, 0, 1);
		spriteObj.transform.localRotation = Quaternion.AngleAxis(Random.Range(-180f, 180f), axis);
	}
}