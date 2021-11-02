using UnityEngine;

public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance;


	/// <summary>
	/// If you override this then make sure you call base.Awake() somewhere in your Awake code.
	/// </summary>
	public virtual void Awake()
	{
		Instance = this as T;
	}


	public virtual void Start()
	{
		Instance = this as T;
	}

	/// <summary>
	/// If you override this then make sure you call base.OnDestroy() somewhere in your OnDestroy code.
	/// </summary>
	protected void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}
}