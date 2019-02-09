using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(ClosetControl))]
public class RegisterCloset : RegisterObject
{
	/// <summary>
	/// Cached closet control for this closet
	/// </summary>
	private ClosetControl closetControl;
	private bool isClosed = true;

	public bool IsClosed
	{
		set
		{
			isClosed = value;
			Passable = !isClosed;
		}
		get { return isClosed; }
	}

	private void Awake()
	{
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