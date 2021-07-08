using UnityEngine;
using System.Collections;

public class DisablePlatformSpecific : MonoBehaviour
{
	[SerializeField] private bool disable = true;
	[SerializeField] private bool destroy = false;

#pragma warning disable 0414
	[Header("Disable this GameObject for these platforms:")]
	[SerializeField] private bool hideOnIOS = false;
	[SerializeField] private bool hideOnAndroid = false;
	[SerializeField] private bool hideOnWindows = false;
	[SerializeField] private bool hideOnMac = false;
	[SerializeField] private bool hideOnLinux = false;
#pragma warning restore 0414

	void Awake()
	{
#if UNITY_IPHONE
		if(hideOnIOS)
#elif UNITY_ANDROID
		if(hideOnAndroid)
#elif UNITY_STANDALONE_WIN
		if(hideOnWindows)
#elif UNITY_STANDALONE_OSX
		if(hideOnMac)
#elif UNITY_STANDALONE_LINUX
		if(hideOnLinux)
#else
		if (false)
#endif
		{
			//Hide because of the platform specification
			HideOrDestroyObject();
		}
	}

	private void HideOrDestroyObject()
	{
		if(disable)
		{
			gameObject.SetActive(false);
		}

		if(destroy)
		{
			Destroy(gameObject);
		}
	}
}