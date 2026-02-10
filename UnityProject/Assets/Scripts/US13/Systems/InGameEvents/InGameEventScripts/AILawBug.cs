using System.Collections.Generic;
using System.Linq;
using US13.Managers.MatrixManager;
using US13.Systems.Ai;
using US13.Systems.Occupations;
using Util;

namespace US13.Systems.InGameEvents.InGameEventScripts
{
	public class AILawBug : EventScriptBase
	{
		public List<string> AIPotentiallaws = new List<string>();


		public override void OnEventStart()
		{
			if (FakeEvent) return;

			var AIs = MatrixManager.MainStationMatrix.Matrix.PresentPlayers.Where(x =>
				x.GetComponentCustom<AiPlayer>() != null);

			if (AIs.Any()) return;
			var AI = AIs.PickRandom();

			var AIPlayer = AI.GetComponentCustom<AiPlayer>();

			var law = AIPotentiallaws.PickRandom();

			if (law.Contains("{RandomRole}"))
			{
				var Occuppation = OccupationList.Instance.Occupations.PickRandom();
				law = law.Replace("{RandomRole}", Occuppation.DisplayName);
			}

			AIPlayer.AddLaw(law, AiPlayer.LawOrder.IonStorm);

			base.OnEventStart();
		}
	}
}