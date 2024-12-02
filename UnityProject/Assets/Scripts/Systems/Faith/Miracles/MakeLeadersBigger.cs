using Scripts.Core.Transform;
using UnityEngine;

namespace Systems.Faith.Miracles
{
	public class MakeLeadersBigger : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Enlarge Leaders";
		[SerializeField] private string faithMiracleDesc = "Makes faith leaders bigger physically.";
		[SerializeField] private SpriteDataSO miracleIcon;

		string IFaithMiracle.FaithMiracleName
		{
			get => faithMiracleName;
			set => faithMiracleName = value;
		}

		string IFaithMiracle.FaithMiracleDesc
		{
			get => faithMiracleDesc;
			set => faithMiracleDesc = value;
		}

		SpriteDataSO IFaithMiracle.MiracleIcon
		{
			get => miracleIcon;
			set => miracleIcon = value;
		}

		public int MiracleCost { get; set; } = 100;
		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			if (invoker == null) return;
			if (invoker.TryGetComponent<ScaleSync>(out var scale) == false) return;
			int scaleNew = Random.Range(2, 3);
			scale.SetScale(new Vector3(scaleNew,scaleNew,scaleNew));
			Chat.AddLocalMsgToChat($"{invoker.visibleName} inflates and becomes bigger", invoker.GameObject);
		}
	}
}