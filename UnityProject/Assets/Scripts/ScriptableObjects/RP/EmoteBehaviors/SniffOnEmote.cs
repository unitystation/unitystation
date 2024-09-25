using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;
using System.Text;
using ScriptableObjects.Atmospherics;

namespace ScriptableObjects.RP.EmoteBehaviors
{
	public class SniffOnEmote : IEmoteBehavior
	{
		public List<GasSO> Blacklist;

		public void Behave(GameObject actor)
		{
			var metaDataLayer = actor.GetComponent<PlayerScript>().RegisterPlayer.Matrix.MetaDataLayer;
			if (metaDataLayer != null)
			{
				var node = metaDataLayer.Get(actor.transform.localPosition.RoundToInt());
				if (node != null)
				{
					Chat.AddExamineMsgFromServer(actor, GetGasMixInfo(node.GasMixLocal));
				}
			}
		}

		private string GetGasMixInfo(GasMix gasMix)
		{
			StringBuilder str = new StringBuilder("You close your eyes and sniff the air.\n");
			bool foundgas = false;
			
			lock (gasMix.GasesArray) //no Double lock
			{
				foreach (var gas in gasMix.GasesArray) //doesn't appear to modify list while iterating
				{
					GasSO gasSo = gas.GasSO;
					
					if (Blacklist.Contains(gasSo))
					{
						continue;
					}
					
					float ratio = gasMix.GasRatio(gasSo);
					//only find gasses on this tile that are not visble
					if (ratio.Approx(0) == false && (gasSo.OverlayTile == null || gas.Moles < gasSo.MinMolesToSee))
					{
						str.AppendLine($"You can smell a hint of {gasSo.Name}.");
						foundgas = true;
					}
				}
			}
			if (foundgas == false)
			{
				str.AppendLine("You do not smell any traces of gas");
			}
			return $"</i>{str}<i>";
		}
	}
}