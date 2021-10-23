using System.Collections.Generic;
using UnityEngine;
using Core.Editor.Attributes;


/// <summary>
/// Component which causes ONLY parent to rotate based on Directional orientation
/// It's mostly useful when you need colider to rotate with Directional (like Wall Protrusions)
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Directional))]
public class DirectionalRotatesParent : MonoBehaviour
{
	[Tooltip("Direction that the children of the root of this prefab are facing in.")]
	[SerializeField, PrefabModeOnly]
	private OrientationEnum prefabChildrenOrientation = OrientationEnum.Down;

	[SerializeField, PrefabModeOnly]
	private bool forceChildrenOpposite;

	public OrientationEnum MappedOrientation
	{
		get { return prefabChildrenOrientation; }
	}

	private void Awake()
	{
		var directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
	}

	private void OnDirectionChanged(Orientation newDir)
	{
		//rotate our sprite renderers based on the deviation from
		//the prefab sprite orientation
		var offset = Orientation.FromEnum(prefabChildrenOrientation).OffsetTo(newDir);

		if (forceChildrenOpposite)
		{
			foreach (Transform child in transform)
			{
				child.localRotation = offset.Quaternion;
			}
		}
	}

	//changes the rendered sprite in editor based on the value set in Directional
#if UNITY_EDITOR
	private void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			var dir = GetComponent<Directional>().InitialOrientation;
			var offset = Orientation.FromEnum(prefabChildrenOrientation).OffsetTo(dir);

			transform.rotation = offset.Quaternion;

			if (forceChildrenOpposite)
			{
				foreach (Transform child in transform)
				{
					child.rotation = Quaternion.Euler(dir.Vector);
				}
			}
		}
	}

	[ContextMenu("ResetChildRotation")]
	private void ResetChildRotation()
	{
		foreach (Transform child in transform)
		{
			child.localRotation = Quaternion.identity;
		}
	}
#endif
}
