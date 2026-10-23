using UnityEngine;

namespace US13.Core.Camera
{
	/// Base class for every full-screen camera effect. The CameraEffectsFeature
	/// finds enabled components of this type on the rendering camera and runs them in Order.
	public abstract class CameraEffectBase : MonoBehaviour
	{
		[SerializeField] private int _order;
		public int Order => _order;

		/// Which pass of the shader to use.
		public virtual int PassIndex => 0;

		/// Push current values to the material and return it. Return null to skip this frame.
		public abstract Material GetMaterial();

		// Runtime instance, so we never modify the shared material asset.
		private Material _instance;
		private Object _source;

		protected Material GetInstance(Material source) => Resolve(source);
		protected Material GetInstance(Shader shader) => Resolve(shader);

		private Material Resolve(Object src)
		{
			if (src == null) return null;
			if (_instance != null && _source == src) return _instance;

			Release();
			_instance = src is Material m ? new Material(m) : new Material((Shader)src);
			_instance.hideFlags = HideFlags.HideAndDontSave;
			_source = src;
			return _instance;
		}

		private void Release()
		{
			if (_instance == null) return;
			if (Application.isPlaying) Destroy(_instance);
			else DestroyImmediate(_instance);
			_instance = null;
			_source = null;
		}

		protected virtual void OnDestroy() => Release();
	}
}