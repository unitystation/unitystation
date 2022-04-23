using UnityEngine;

public class RcsThruster : MonoBehaviour
{
	public ParticleSystem thrusterParticles;

	public delegate void OnThrusterDestroyedDelegate();
	public OnThrusterDestroyedDelegate OnThrusterDestroyedEvent;

	public Rotatable rotatable;

	private void OnDestroy()
	{
		// remove this thruster from RCS thruster list when destroyed
		OnThrusterDestroyedEvent?.Invoke();
	}
}
