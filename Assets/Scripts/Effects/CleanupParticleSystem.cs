using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanupParticleSystem : MonoBehaviour
{

    void Start()
    {
        var duration = GetComponent<ParticleSystem>().main.duration;
        Destroy(this, duration);
    }
}
