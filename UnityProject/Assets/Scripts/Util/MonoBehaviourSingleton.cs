
using UnityEngine;

public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	private static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<T>();
			}

			return instance;
		}
	}

	/// <summary>
	/// There can be only one of each singleton. If you override this then make sure you call base.Awake() somewhere in your Awake code.
	/// </summary>
	protected virtual void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this);
		}
	}

	protected virtual void OnDestroy()
	{
		if (Instance == this)
		{
			instance = null;
		}	
	}
}