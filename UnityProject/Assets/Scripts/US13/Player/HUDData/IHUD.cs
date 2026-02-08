using UnityEngine;

namespace US13.Player.HUDData
{
	public interface IHUD
	{
		public GameObject Prefab { get; set; }

		public GameObject InstantiatedGameObject { get; set; }

		public void SetUp();


		public void SetVisible(bool visible);

	}
}
