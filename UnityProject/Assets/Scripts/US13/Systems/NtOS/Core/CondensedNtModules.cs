using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Systems.NtOS.Core
{
	[RequireComponent(typeof(NtOS_Device))]
	public class CondensedNtModules : MonoBehaviour
	{
		[SerializeReference, SelectImplementation(typeof(INtOSModule))]
		public List<INtOSModule> ModulesToAdd = new List<INtOSModule>();
	}
}