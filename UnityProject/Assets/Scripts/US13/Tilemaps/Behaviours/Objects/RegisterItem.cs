using UnityEngine;
using UnityEngine.Events;

namespace US13.Tilemaps.Behaviours.Objects
{
	[System.Serializable]
	public class OnCrossed : UnityEvent<RegisterPlayer>{};

	[ExecuteInEditMode]
	public class RegisterItem : RegisterTile
	{
//	public OnCrossed crossed;
//
//	public void Cross(RegisterPlayer registerPlayer)
//	{
//		crossed.Invoke(registerPlayer);
//	}

	}
}