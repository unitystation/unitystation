using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Research;

namespace ScriptableObjects.Systems.Research
{
	[CreateAssetMenu(fileName = "ArtifactConsoleDataSO", menuName = "ScriptableObjects/Systems/Research/ArtifactConsoleDataSO")]
	public class ArtifactConsoleDataSO : ScriptableObject
	{
		public List<EffectIndex> AreaEffects = new List<EffectIndex>();
		public List<EffectIndex> ContactEffects = new List<EffectIndex>();
		public List<EffectIndex> FeedEffects = new List<EffectIndex>();
		public List<EffectIndex> DamageEffects = new List<EffectIndex>();
		public List<EffectIndex> GasReactEffects = new List<EffectIndex>();
	}
}
