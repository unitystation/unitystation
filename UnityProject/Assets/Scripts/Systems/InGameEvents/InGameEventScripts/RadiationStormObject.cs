using System.Collections;
using UnityEngine;
using Systems.Radiation;
using NaughtyAttributes;

namespace InGameEvents
{
	[RequireComponent(typeof(RadiationProducer))]
	public class RadiationStormObject : MonoBehaviour
	{
		[Tooltip("The amount of radiation this source should emit at peak size.")]
		[SerializeField, Range(10000, 100000)]
		private int peakRadiation = 30000;

		[Tooltip("How long this source takes to grow to full size (randomly selected within range).")]
		[SerializeField, MinMaxSlider(0, 10)]
		private Vector2 randomGrowthTime = new Vector2(2, 5);

		[Tooltip("How long this source should live in total (randomly selected within range).")]
		[SerializeField, MinMaxSlider(30, 120)]
		private Vector2 lifeTime = new Vector2(30, 60);

		private RadiationProducer radiator;

		private float growthTime;
		private float time;
		private bool isGrowing = true;

		private void Awake()
		{
			radiator = GetComponent<RadiationProducer>();

			growthTime = Random.Range(randomGrowthTime.x, randomGrowthTime.y);
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateRadiationOutput);
			}
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateRadiationOutput);
			}
		}

		private void Start()
		{
			float degrees = 720;
			gameObject.LeanRotateZ(degrees, 20).setLoopClamp().setOnComplete(() =>
			{
				degrees += 360;
			});

			StartCoroutine(DelayDecay());
		}

		private void UpdateRadiationOutput()
		{
			if (isGrowing && time > growthTime) return;
			if (isGrowing == false && time <= 0) return;

			time += isGrowing ? Time.deltaTime : -Time.deltaTime;
			radiator.SetLevel(peakRadiation * (time / growthTime));
		}

		private IEnumerator DelayDecay()
		{
			yield return WaitFor.Seconds(Random.Range(lifeTime.x, lifeTime.y) - growthTime);
			isGrowing = false;
			yield return WaitFor.Seconds(growthTime);
			Despawn.ServerSingle(gameObject);
		}
	}
}
