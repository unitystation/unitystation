using Mirror;

public class ManagedNetworkBehaviour : NetworkBehaviour
{
	protected bool IsUpdating;

	protected virtual void OnEnable()
	{
		UpdateManager.Add(this);
		IsUpdating = true;
	}

	protected virtual void OnDisable()
	{
		UpdateManager.Remove(this);
		IsUpdating = false;
	}

	/// <summary>
	///     If your class uses the Awake function, please use  protected override void Awake() instead.
	///     Also don't forget to call ManagedNetworkBehaviour.Awake(); first.
	///     If your class does not use the Awake function, this object will be added to the UpdateManager automatically.
	///     Do not forget to replace your Update function with public override void UpdateMe()
	/// </summary>
	public virtual void UpdateMe()
	{
	}

	/// <summary>
	///     If your class uses the Awake function, please use  protected override void Awake() instead.
	///     Also don't forget to call OverridableMonoBehaviour.Awake(); first.
	///     If your class does not use the Awake function, this object will be added to the UpdateManager automatically.
	///     Do not forget to replace your Fixed Update function with public override void FixedUpdateMe()
	/// </summary>
	public virtual void FixedUpdateMe()
	{
	}

	/// <summary>
	///     If your class uses the Awake function, please use  protected override void Awake() instead.
	///     Also don't forget to call OverridableMonoBehaviour.Awake(); first.
	///     If your class does not use the Awake function, this object will be added to the UpdateManager automatically.
	///     Do not forget to replace your Late Update function with public override void LateUpdateMe()
	/// </summary>
	public virtual void LateUpdateMe()
	{
	}
}
