using UnityEngine;

public class CleanupParticleSystem : MonoBehaviour
{
	private void Start()
	{
		float duration = GetComponent<ParticleSystem>().main.duration;
		Destroy(this, duration);
	}
}