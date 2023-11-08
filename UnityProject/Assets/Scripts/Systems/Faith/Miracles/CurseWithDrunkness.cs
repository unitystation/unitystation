using HealthV2;
using UnityEngine;
using Util.Independent.FluentRichText;

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

		public void DoMiracle(FaithData associatedFaith, PlayerScript invoker = null)
		{
			foreach (var leader in associatedFaith.FaithLeaders)
			{
				Chat.AddLocalMsgToChat($"{leader.visibleName}'s eyes become white as they start chanting some words loudly..", leader.gameObject);
				Chat.AddChatMsgToChatServer(leader.PlayerInfo, "..Gin-La tok.. Ja-kra-ko..", ChatChannel.Local, Loudness.LOUD);
				if (DMMath.Prob(50))
				{
					MakePersonDrunk(leader.playerHealth);
					Chat.AddExamineMsg(leader.gameObject, "The curse misfires and affects you as well!");
				}
				var overlapBox =
					Physics2D.OverlapBoxAll(leader.gameObject.AssumedWorldPosServer(), new Vector2(6, 6), 0);
				foreach (var collider in overlapBox)
				{
					if (collider.TryGetComponent<LivingHealthMasterBase>(out var health) == false) continue;
					if (MatrixManager.Linecast(leader.AssumedWorldPos,
						    LayerTypeSelection.Walls, LayerMask.GetMask("Walls"),
						    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
					MakePersonDrunk(health);
				}
			}
		}

		private void MakePersonDrunk(LivingHealthMasterBase health)
		{
			if (health.brain == null || health.brain.ReagentCirculatedComponent?.AssociatedSystem?.BloodPool == null) return;
			health.brain.ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(health.brain.DrunkReagent, 100);
			string msg = new RichText().Italic().Color(RichTextColor.Red).Add("You feel like a drunkard out of nowhere..");
			Chat.AddExamineMsg(health.gameObject, msg);
		}
	}
}