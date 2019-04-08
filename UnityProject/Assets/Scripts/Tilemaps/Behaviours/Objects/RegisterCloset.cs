using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(ClosetControl))]
public class RegisterCloset : RegisterObject
{

	[Tooltip("Type of closet, affects passability / collision detection when open vs. closed.")]
	public ClosetType closetType = ClosetType.LOCKER;

	/// <summary>
	/// Cached closet control for this closet
	/// </summary>
	private ClosetControl closetControl;
	private bool isClosed = true;
	// cached colliders so they can be disabled
	private Collider2D[] colliders;

	public bool IsClosed
	{
		set
		{
			isClosed = value;
			if (closetType == ClosetType.LOCKER)
			{
				//disable colliders and make passable when open, for lockers only
				Passable = !isClosed;
				foreach (var collider in colliders)
				{
					collider.enabled = !Passable;
				}
			}
		}
		get => isClosed;
	}

	private void Awake()
	{
		colliders = GetComponents<Collider2D>();
		closetControl = GetComponent<ClosetControl>();
	}

	protected override void OnParentChangeComplete()
	{
		if (closetControl != null)
		{
			// update the parent of each of the items in the closet
			closetControl.OnParentChangeComplete(ParentNetId);
		}
	}
}

public enum ClosetType
{
	LOCKER,
	CRATE
}