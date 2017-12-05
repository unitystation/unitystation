using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShroudTile : MonoBehaviour
{

    public Renderer renderer;

    void OnEnable()
    {
        renderer.enabled = true;
    }

    public void SetShroudStatus(bool enabled)
    {
        renderer.enabled = enabled;
    }

}
