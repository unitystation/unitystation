using System.Runtime.CompilerServices;
using UnityEngine;

public interface IStoreThings
{

	public bool ServerTryAdd(GameObject inGameObject, bool IgnoreRestraints = false);
	public bool CanFit(GameObject inGameObject);

	//ServerAdd

	//CanFit
}
