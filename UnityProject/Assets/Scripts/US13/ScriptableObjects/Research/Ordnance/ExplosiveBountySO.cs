using System.Collections.Generic;
using UnityEngine;

namespace US13.ScriptableObjects.Research.Ordnance
{
	[CreateAssetMenu(fileName = "ExplosiveBountyList", menuName = "ScriptableObjects/Systems/Research/ExplosiveBountyList")]
	public class ExplosiveBountySO : ScriptableObject
	{
		[field: SerializeField] public List<ExplosiveBounty> PossibleBounties { get; private set;}
	}
}
