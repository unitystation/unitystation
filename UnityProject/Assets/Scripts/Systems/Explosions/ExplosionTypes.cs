using System.Collections;
using System.Collections.Generic;
using Systems.Explosions;

//explosion types available to ExplosionComponent and ChemExplosion, you don't have to put your own explosion type here, but this will make it available to those components
public class ExplosionTypes
{
	public enum ExplosionType //add your explosion type here
	{
		Regular,
		EMP
	}

	public static Dictionary<ExplosionType, ExplosionNode> NodeTypes = new Dictionary<ExplosionType, ExplosionNode>() //add your node type here
	{
			{ExplosionType.Regular, new ExplosionNode()},
			{ExplosionType.EMP, new ExplosionEmpNode()}
	};
}
