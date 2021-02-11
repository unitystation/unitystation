using UnityEngine;

	public class RotateAroundTransform : MonoBehaviour
	{
		[SerializeField]
		private Transform transformToRotateAround = null;

		private Transform ourTransform;

		private void Awake()
		{
			ourTransform = transform;
		}

		void Update()
		{
			ourTransform.RotateAround(transformToRotateAround.position, transformToRotateAround.up, 100*Time.deltaTime);
		}
	}