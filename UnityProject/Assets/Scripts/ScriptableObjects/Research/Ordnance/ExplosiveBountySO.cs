using System.Collections.Generic;
using UnityEngine;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "ExplosiveBountyList", menuName = "ScriptableObjects/Systems/Research/ExplosiveBountyList")]
	public class ExplosiveBountySO : ScriptableObject
	{
		[field: SerializeField] public List<ExplosiveBounty> PossibleBounties { get; private set;}
	}
}
