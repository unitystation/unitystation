using System;
using UnityEngine;

namespace Core.Camera
{
	public class UVFollow : MonoBehaviour
	{
		private MeshRenderer mesh;
		private Material mat;

		public float speed = 2f;

		private void Awake()
		{
			mesh = GetComponent<MeshRenderer>();
			mat = mesh.material;
		}

		private void FixedUpdate()
		{
			if(PlayerManager.LocalPlayerObject == null) return;
			Vector2 offset = mat.mainTextureOffset;
			offset.x = PlayerManager.LocalPlayerObject.transform.position.x / transform.localScale.x / speed;
			offset.y = PlayerManager.LocalPlayerObject.transform.position.y / transform.localScale.y / speed;

			mat.mainTextureOffset = offset;
		}
	}
}