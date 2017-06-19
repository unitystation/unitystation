using UnityEngine;
using System.Collections;
using PicoGames.VLS2D;

public class VLSFlicker : MonoBehaviour 
{
    public VLSLight vlsLight = null;
    public Gradient colorGradient = new Gradient();
    public float speedMin = 0.1f;
    public float speedMax = 0.2f;

    void OnEnable()
    {
        if (vlsLight == null)
            return;

        StartCoroutine(Flicker());
    }

    IEnumerator Flicker()
    {
        while(enabled)
        {
            yield return new WaitForSeconds(Random.Range(speedMin, speedMax));
            vlsLight.Color = colorGradient.Evaluate(Random.Range(0f, 1f));
        }
    }
}
