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
		public void DoMiracle()
		{
			foreach (var member in FaithManager.Instance.FaithMembers)
			{
				Chat.AddLocalMsgToChat($"A red tether appears from {member.visibleName} to nearby objects..", member.gameObject);
				var overlapBox =
					Physics2D.OverlapBoxAll(member.gameObject.AssumedWorldPosServer(), new Vector2(5, 5), 0);
				foreach (var collider in overlapBox)
				{
					if (MatrixManager.Linecast(member.AssumedWorldPos,
						    LayerTypeSelection.Walls, LayerMask.GetMask("Walls"),
						    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
					if (collider.TryGetComponent<Integrity>(out var integrity) == false) continue;
					integrity.OnDamaged.AddListener( () => Explosion.StartExplosion(integrity.gameObject.AssumedWorldPosServer().CutToInt(), 35f) );
				}
			}
		}
	}
}