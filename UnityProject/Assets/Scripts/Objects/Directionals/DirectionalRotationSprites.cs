
using System;
using UnityEngine;

/// <summary>
/// Component which causes an object's sprites to rotate based on its
/// current direction.
/// Use this for things like shuttle heaters and wall protrusions which should change their sprite direction based on the rotation
/// of their parent matrix.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Directional))]
public class DirectionalRotationSprites : MonoBehaviour
{
	[Tooltip("Direction that the sprites in the sprite renderers of this prefab are facing in.")]
	[SerializeField]
	private OrientationEnum prefabSpriteOrientation = OrientationEnum.Down;

	private SpriteRenderer[] spriteRenderers;

	private void Awake()
	{
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		var directional = GetComponent<Directional>();
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
		OnDirectionChanged(directional.CurrentDirection);
	}

	private void OnDirectionChanged(Orientation newDir)
	{
		//rotate our sprite renderers based on the deviation from
		//the prefab sprite orientation
		var offset = Orientation.FromEnum(prefabSpriteOrientation).OffsetTo(newDir);

		foreach (var spriteRenderer in spriteRenderers)
		{
			spriteRenderer.transform.rotation = offset.Quaternion;
		}
	}

	//changes the rendered sprite in editor based on the value set in Directional
#if UNITY_EDITOR
	private void Update()
	{
		if (Application.isEditor && !Application.isPlaying)
		{
			var dir = GetComponent<Directional>().InitialOrientation;
			var offset = Orientation.FromEnum(prefabSpriteOrientation).OffsetTo(dir);

			foreach (var spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.rotation = offset.Quaternion;
			}
		}
	}
#endif
}
