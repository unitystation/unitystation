using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Research/ArtifactEffects/EffectsCollection")]
public class ArtifactEffectsCollection : ScriptableObject
{
	public ArtifactEffect[] Effects;
}
