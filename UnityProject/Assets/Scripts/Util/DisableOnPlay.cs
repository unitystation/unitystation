using UnityEngine;

public class DisableOnPlay : MonoBehaviour
{
	private void Start()
	{
		gameObject.SetActive(false);
	}
}