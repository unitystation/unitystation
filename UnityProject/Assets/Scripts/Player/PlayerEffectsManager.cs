using UnityEngine;
using System.Collections.Generic;

public class PlayerEffectsManager : MonoBehaviour
{
    private NetworkedLeanTween tween;

    [Tooltip("All effects that can be played on this player.")]
    public List<LTEffect> Effects;

    private void Awake() 
    {
        tween = GetComponent<NetworkedLeanTween>();
    }

    public void StartEffect(string effectName)
    {
        foreach (var e in Effects)
        {
            print(e.name);
            if(e.name == effectName)
            {
                var effect = GetComponent(effectName) as LTEffect;
                effect.CmdStartAnimation();
            }
        }
    }
}
