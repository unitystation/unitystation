using UnityEngine;

public class CleanupParticleSystem : MonoBehaviour
{
    private void Start()
    {
        var duration = GetComponent<ParticleSystem>().main.duration;
        Destroy(this, duration);
    }
}