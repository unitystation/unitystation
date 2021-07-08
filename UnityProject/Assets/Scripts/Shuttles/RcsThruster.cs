using UnityEngine;

public class RcsThruster : MonoBehaviour
{
	public ParticleSystem thrusterParticles;
	public DirectionalRotatesParent directional;

	public delegate void OnThrusterDestroyedDelegate();
	public OnThrusterDestroyedDelegate OnThrusterDestroyedEvent;
	
	private void OnDestroy()
	{
		// remove this thruster from RCS thruster list when destroyed
		OnThrusterDestroyedEvent?.Invoke();
	}
}
