using System.Collections;
using System.Collections.Generic;
using UnityEngine;

	public class ThrowableBreakable : MonoBehaviour
{
	[SerializeField]
	private GameObject BrokenItem = default;

	private CustomNetTransform customNetTransform;

	private void Start()
	{
		customNetTransform = GetComponent<CustomNetTransform>();
		customNetTransform.OnThrowEnd.AddListener(MyListener);
	}

	private void OnDisable()
	{
		customNetTransform.OnThrowEnd.RemoveListener(MyListener);
	}

	private void MyListener(ThrowInfo info)
	{
		Spawn.ServerPrefab(BrokenItem, gameObject.AssumedWorldPosServer());
		Despawn.ServerSingle(gameObject);
		SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.GlassBreak01, gameObject.AssumedWorldPosServer());
	}

}
