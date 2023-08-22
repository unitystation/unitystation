using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MouseIconSo", menuName = "ScriptableObjects/UI/MouseIconSo")]
public class MouseIconSo : ScriptableObject
{
	public Texture2D Texture = null;
	public Vector2 Offset = Vector2.zero;
}
