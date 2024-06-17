using Mirror;
using Objects.Atmospherics;
using UnityEngine;

namespace Items.Weapons
{
	[RequireComponent(typeof(GasContainer))]
	[RequireComponent(typeof(Grenade))]
	public class AtmosGrenade : NetworkBehaviour, IExaminable
	{
		[Tooltip("SpriteHandler used for gas color overlay")]
		public SpriteHandler gasIndicatorSpriteHandler;
	
		private GasContainer gasContainer;
		private Grenade grenade;
		
		private readonly Color noGasColor = new(0,0,0,0);

		public void Start()
		{
			gasContainer = GetComponent<GasContainer>();
			grenade = GetComponent<Grenade>();
			UpdateOverlay();
			grenade.OnExpload.AddListener(ReleaseGas);
			gasContainer.OnContentsUpdate += UpdateOverlay;
		}

		private void OnDestroy()
		{
			grenade.OnExpload.RemoveListener(ReleaseGas);
			gasContainer.OnContentsUpdate -= UpdateOverlay;
		}
		
		public void ReleaseGas(){
			gasContainer.ReleaseContentsInstantly();
			UpdateOverlay();
		}

		private void UpdateOverlay()
		{
			if (isServer)
			{
				var gasCont = gasContainer.GasMixLocal.GetBiggestGasSOInMix();
				if (gasCont != null)
				{
					//gas SO's color property is for overriding the overlay tile color, use the reagents color and force alpha to be 255
					var gasCol = gasCont.AssociatedReagent.color;
						gasCol.a = 1f;
					gasIndicatorSpriteHandler.SetColor(gasCol);
				}
				else
				{
					gasIndicatorSpriteHandler.SetColor(noGasColor);
				}
			}
		}
		
		public string Examine(Vector3 pos)
		{
			return gasContainer.GasMixLocal.Pressure > 50 ? "Gas swirls around inside." : "It's empty." ;
		}
	}	
}
