
using Lighting;
using UnityEngine;

public class LightHealthBehaviour : HealthBehaviour
{

    public override void onDeathActions()
    {
//        Debug.Log("Light ded!");
        GetComponentInParent<LightSource>().Trigger(false); //insert better solution here
    }
}
