using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(ClosetControl))]
public class RegisterCloset : RegisterObject
{

	[Tooltip("Type of closet, affects passability / collision detection when open vs. closed.")]
	public ClosetType closetType;

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
			if (closetType == ClosetType.LOCKER || closetType == ClosetType.SCANNER)
			{
				//become passable to bullets and people when open
				Passable = !isClosed;
				//switching to item layer if open so bullets pass through it
				if (Passable)
				{
					gameObject.layer = LayerMask.NameToLayer("Items");
				}
				else
				{
					gameObject.layer = LayerMask.NameToLayer("Machines");
				}

			}
		}
		get => isClosed;
	}

	private void Awake()
	{
		base.Awake();
		closetControl = GetComponent<ClosetControl>();
		OnParentChangeComplete.AddListener(ReparentContainedObjectsOnParentChangeComplete);
	}

	private void ReparentContainedObjectsOnParentChangeComplete()
	{
		if (closetControl != null)
		{
			// update the parent of each of the items in the closet
			closetControl.OnParentChangeComplete(GrandparentMatrixNetId);
		}
	}
}

public enum ClosetType
{
	LOCKER,
	CRATE,
	SCANNER,
	OTHER
}