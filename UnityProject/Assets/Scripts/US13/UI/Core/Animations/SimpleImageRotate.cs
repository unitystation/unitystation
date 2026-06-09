using UnityEngine;
using UnityEngine.Serialization;
using US13.Managers.UpdateManager;

namespace US13.UI.Core.Animations
{
	public class SimpleImageRotate : MonoBehaviour
	{
		private bool rotating = false;

		private float Speed = 180;

		[Tooltip("Degrees per second") ,FormerlySerializedAs("Speed")]
		public float InitialSpeed = 180;

		public bool randomise = false;

		private void OnEnable()
		{
			if (randomise)
			{
				Speed = Random.Range(-180f, 180f);
			}
			else
			{
				Speed = InitialSpeed;
			}
			rotating = true;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			rotating = false;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (rotating)
			{
				transform.Rotate(0, 0, Speed * Time.deltaTime);
			}
		}
	}
}