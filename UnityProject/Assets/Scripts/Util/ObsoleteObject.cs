using Logs;
using UnityEngine;

namespace Util
{
	public class ObsoleteObject : MonoBehaviour
	{
		private UniversalObjectPhysics objectPhysics;

		private void Awake()
		{
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private void Start()
		{
			Loggy.LogError($"Obsolete object: {gameObject.ExpensiveName()} on matrix: {objectPhysics.registerTile.Matrix} at world coord: {objectPhysics.OfficialPosition}. Please remove from scene");
		}
	}
}