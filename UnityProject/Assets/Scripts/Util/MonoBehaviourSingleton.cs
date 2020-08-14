
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
	/// If you override this then make sure you call base.Awake() somewhere in your Awake code.
	/// </summary>
	protected virtual void Awake()
	{
		if (Instance != null && Instance != this)
		{
#if UNITY_EDITOR
			DestroyImmediate(this);
#else
			Destroy(this);
#endif
		}
	}

	/// <summary>
	/// If you override this then make sure you call base.OnDestroy() somewhere in your OnDestroy code.
	/// </summary>
	protected virtual void OnDestroy()
	{
		if (Instance == this)
		{
			instance = null;
		}	
	}
}