using Systems.Explosions;
using UnityEngine;

namespace Systems.Faith.Miracles
{
	public class RigObjects : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Rig Objects";
		[SerializeField] private string faithMiracleDesc = "Causes objects nearby faith members to be <i>slightly</i> explosive upon receiving any damage.";
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

		public int MiracleCost { get; set; } = 300;
		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			if (invoker == null) return;
			Chat.AddLocalMsgToChat($"A red tether appears from {invoker.visibleName} to nearby objects..", invoker.gameObject);
			var overlapBox =
				Physics2D.OverlapBoxAll(invoker.gameObject.AssumedWorldPosServer(), new Vector2(5, 5), 0);
			foreach (var collider in overlapBox)
			{
				if (MatrixManager.Linecast(invoker.AssumedWorldPos,
					    LayerTypeSelection.All, LayerMask.GetMask("Walls"),
					    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
				if (collider.TryGetComponent<Integrity>(out var integrity) == false) continue;
				SparkUtil.TrySpark(integrity.gameObject);
				integrity.OnDamaged.AddListener( () =>
				{
					Explosion.StartExplosion(integrity.gameObject.AssumedWorldPosServer().CutToInt(), 35f);
					if (DMMath.Prob(35))
					{
						integrity.OnDamaged.RemoveAllListeners();
					}
					SparkUtil.TrySpark(integrity.gameObject);
				});
			}
		}
	}
}