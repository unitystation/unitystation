using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

namespace _3D
{
	public class SkyboxData : SingletonManager<SkyboxData>
	{
		public List<Material> SkyboxMaterials = new List<Material>();
	}
}