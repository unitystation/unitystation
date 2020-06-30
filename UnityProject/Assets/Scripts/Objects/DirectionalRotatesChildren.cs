using System;
using UnityEngine;

/// <summary>
/// Component which causes all of an object's children (not just sprites, so also would affect particle systems, light2d, etc...) to rotate based on its
/// current direction.
/// Use this for things like thrusters which should change their sprites / lights / effects direction based on the rotation
/// of their parent matrix. NOTE: If you need to set a pivot point for these things (as is common with light2d and
/// particle systems), you can simply create an empty child transform at the pivot point and parent the light2d / particle
/// system transforms under that. See shuttle engine prefabs for example.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Directional))]
public class DirectionalRotatesChildren : MonoBehaviour
{
	[Tooltip("Direction that the children of the root of this prefab are facing in.")]
	[SerializeField]
	private OrientationEnum prefabChildrenOrientation = OrientationEnum.Down;

	private void Awake()
	{
		var directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
		OnDirectionChanged(directional.CurrentDirection);
	}

	public void EditorDirectionChanged()
	{
		var dir = GetComponent<Directional>().InitialOrientation;
		var offset = Orientation.FromEnum(prefabChildrenOrientation).OffsetTo(dir);

		foreach (Transform child in transform)
		{
			child.rotation = offset.Quaternion;
		}
	}

	private void OnDirectionChanged(Orientation newDir)
	{
		//rotate our sprite renderers based on the deviation from
		//the prefab sprite orientation
		var offset = Orientation.FromEnum(prefabChildrenOrientation).OffsetTo(newDir);

		foreach (Transform child in transform)
		{
			child.rotation = offset.Quaternion;
		}
	}

	//changes the rendered sprite in editor based on the value set in Directional
#if UNITY_EDITOR
	private void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			EditorDirectionChanged();
		}
	}
#endif
}