using UnityEngine;

namespace US13.Core.Input_System
{
	[CreateAssetMenu(fileName = "MouseIconSo", menuName = "ScriptableObjects/UI/MouseIconSo")]
	public class MouseIconSo : ScriptableObject
	{
		public Texture2D Texture = null;
		public Vector2 Offset = Vector2.zero;
	}
}
