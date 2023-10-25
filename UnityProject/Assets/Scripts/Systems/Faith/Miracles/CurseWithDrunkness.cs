using HealthV2;
using UnityEngine;
using Util.Independent.FluentRichText;
using Color = Util.Independent.FluentRichText.Color;

namespace Systems.Faith.Miracles
{
	public class CurseWithDrunkness : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Curse with Drunkness.";
		[SerializeField] private string faithMiracleDesc = "Causes all humanoids within a small circle oh all faith leaders to become drunk.";
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

		public int MiracleCost { get; set; } = 350;

		public void DoMiracle()
		{
			foreach (var leader in FaithManager.Instance.FaithLeaders)
			{
				var overlapBox =
					Physics2D.OverlapBoxAll(leader.gameObject.AssumedWorldPosServer(), new Vector2(6, 6), 0);
				foreach (var collider in overlapBox)
				{
					if (collider.TryGetComponent<LivingHealthMasterBase>(out var health) == false) continue;
					if (MatrixManager.Linecast(leader.AssumedWorldPos,
						    LayerTypeSelection.Walls, LayerMask.GetMask("Walls"),
						    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
					if (health.brain == null) continue;
					health.brain.SyncDrunkenness(health.brain.DrunkAmount, 100);
					string msg = new RichText("You feel like a drunkard out of nowhere..").Italic().Color(Color.Red);
					Chat.AddExamineMsg(health.gameObject, msg);
				}
			}
		}
	}
}