using UnityEngine;

namespace Systems.Faith
{
	public interface IFaithMiracle
	{
		public string FaithMiracleName { get; protected set; }
		public string FaithMiracleDesc { get; protected set; }
		public SpriteDataSO MiracleIcon { get; protected set; }

		public int MiracleCost { get; set; }
		public void DoMiracle();
	}
}