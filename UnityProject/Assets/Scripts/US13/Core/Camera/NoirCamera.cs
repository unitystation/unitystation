using UnityEngine;

namespace US13.Core.Camera
{
	[ExecuteInEditMode]
	public class NoirCamera : MonoBehaviour
	{
		// Public data
		public Shader shader;
		[Range(0.5f, 2f)]
		public float lensRadius = 0.5f;

		private const float MAX_LENS_RADIUS = 4f;
		// Private data
		Material _material;

		private bool lensRadiusMaxed = false;


#if UNITY_EDITOR
		private void OnValidate()
		{
			if (_material != null)
			{
				DestroyImmediate(_material);
				_material = null;
			}
			UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}
#endif


		// Called by Camera to apply image effect
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (shader == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			if (_material == false)
			{
				_material = new Material(shader);
			}

			if (lensRadiusMaxed == false) _material.SetFloat("_LensRadius", lensRadius);
			else _material.SetFloat("_LensRadius", MAX_LENS_RADIUS);

			Graphics.Blit(source, destination, _material);
		}

		public void HasMaxedLensRadius(bool set)
		{
			lensRadiusMaxed = set;
		}
	}
}