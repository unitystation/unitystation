using UnityEngine;

	public class RotateAroundTransform : MonoBehaviour
	{
		[SerializeField]
		private Transform transformToRotateAround = null;

		private Transform ourTransform;
		private int time = 100;

		[SerializeField] private Vector3 offset = new Vector3(0, 0, 0);

		public Transform TransformToRotateAround
		{
			get => transformToRotateAround;
			set => transformToRotateAround = value;
		}

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
			if(transformToRotateAround == null) return;
			ourTransform.RotateAround(transformToRotateAround.gameObject.AssumedWorldPosServer() + offset, transformToRotateAround.forward, time*Time.deltaTime);
		}
	}