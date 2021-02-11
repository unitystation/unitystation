using UnityEngine;

	public class RotateAroundTransform : MonoBehaviour
	{
		[SerializeField]
		private Transform transformToRotateAround = null;

		private Transform ourTransform;
		private int time = 100;

		private void Awake()
		{
			ourTransform = transform;
			time = Random.Range(50, 100);
		}

		void Update()
		{
			ourTransform.RotateAround(transformToRotateAround.position, transformToRotateAround.forward, time*Time.deltaTime);
		}
	}