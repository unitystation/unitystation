using Mirror;
using UnityEngine;

public class IntegrityGamemodeCheck : NetworkBehaviour
{
	[SerializeField]
	[Tooltip("Check this if you want this object on this matrix to not be able to take damage unless on certain round types")]
	private bool indestructableNotOnRound;

	[SerializeField]
	[Tooltip("Any round in this list will make the matrix destructable")]
	private GameMode roundToBeDestructable;

	[Server]
	// This simply checks if the bool is true, if not the door will not be affected by rounds
	private void Start()
	{
		if (indestructableNotOnRound)
		{
			var component = gameObject.GetComponent<Integrity>();
			DestructableRoundCheck(component);
		}
	}
	[Server]
	//Will check the gameManger's current gamemode against its list, if it returns true then it will make the door indestructable
	//Or anything with integrity for that matter
	private void DestructableRoundCheck(Integrity currentObject)
	{
		if (GameManager.Instance.GetGameModeName(true) != roundToBeDestructable.Name)
		{
			currentObject.Resistances.Indestructable = true;
		}
	}
}