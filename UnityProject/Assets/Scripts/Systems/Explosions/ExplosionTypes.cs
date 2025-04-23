using System.Collections;
using System.Collections.Generic;
using Systems.Explosions;
using UnityEngine;

//explosion types available to ExplosionComponent and ChemExplosion, you don't have to put your own explosion type here, but this will make it available to those components
public class ExplosionTypes
{
	public enum ExplosionType //add your explosion type here
	{
		Regular,
		EMP,
		PlayerFriendly,
		Harmless,
		DarkMatter
	}

	public static readonly Dictionary<ExplosionType, ExplosionNode> NodeTypes = new Dictionary<ExplosionType, ExplosionNode>() //add your node type here
	{
			{ExplosionType.Regular, new ExplosionNode(Vector3.zero)},
			{ExplosionType.EMP, new ExplosionEmpNode(Vector3.zero)},
			{ExplosionType.PlayerFriendly, new PlayerFriendlyExplosionNode(Vector3.zero)},
			{ExplosionType.Harmless, new HarmlessExplosionNode(Vector3.zero)},
			{ExplosionType.DarkMatter, new DarkMatterExplosionNode(Vector3.zero)}
	};
}
