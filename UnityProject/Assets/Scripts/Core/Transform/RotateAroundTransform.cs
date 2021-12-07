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

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			ourTransform.RotateAround(transformToRotateAround.position, transformToRotateAround.forward, time*Time.deltaTime);
		}
	}