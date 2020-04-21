using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Extended")]
public class Extended : GameMode
{
	protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
	{
		//no antags
		return false;
	}
}