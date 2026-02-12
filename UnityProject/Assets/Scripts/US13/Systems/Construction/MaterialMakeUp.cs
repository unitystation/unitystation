using UnityEngine;
using US13.Items;

namespace US13.Systems.Construction
{
	public class MaterialMakeUp : MonoBehaviour
	{
		public SerializableDictionary<MaterialSheet, int> MakeUp = new SerializableDictionary<MaterialSheet, int>();
	}
}
