using Objects.Botany;
using UnityEngine;

namespace Systems.Faith.Miracles
{
	public class BlessCrops : IFaithMiracle
	{
		[SerializeField] private string faithMiracleName = "Bless Crops";
		[SerializeField] private string faithMiracleDesc = "Pray for a blessing upon nearby crops.";
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

		public int MiracleCost { get; set; } = 375;
		public void DoMiracle()
		{
			foreach (var farmer in FaithManager.Instance.FaithMembers)
			{
				Chat.AddLocalMsgToChat($"{farmer.visibleName}'s eyes become white as they start chanting some words loudly while small vines ever so slowly wrap around them..", farmer.gameObject);
				Chat.AddChatMsgToChatServer(farmer.PlayerInfo, "..Banana... ro-TA-te..", ChatChannel.Local, Loudness.LOUD);
				var overlapBox =
					Physics2D.OverlapBoxAll(farmer.gameObject.AssumedWorldPosServer(), new Vector2(12, 12), 0);
				foreach (var collider in overlapBox)
				{
					if (MatrixManager.Linecast(farmer.AssumedWorldPos,
						    LayerTypeSelection.Walls, LayerMask.GetMask("Walls"),
						    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
					if (collider.TryGetComponent<HydroponicsTray>(out var tray) == false) continue;
					BlessPlant(tray);
				}
			}
		}

		private void BlessPlant(HydroponicsTray tray)
		{
			if (tray.HasPlant == false) return;
			if (DMMath.Prob(35)) tray.PlantData.Mutation();
			tray.PlantData.PlantName = $"Blessed {tray.PlantData.PlantName}";
			tray.PlantData.Health = 2250;
			tray.PlantData.Yield = 125;
			tray.PlantData.GrowthSpeed = 900;
		}
	}
}