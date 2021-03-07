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
}
