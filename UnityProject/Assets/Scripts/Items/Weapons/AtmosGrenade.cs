using Objects.Atmospherics;
using UnityEngine;

namespace Items.Weapons
{
	[RequireComponent(typeof(GasContainer))]
	public class AtmosGrenade : Grenade, IServerInventoryMove, IExaminable
	{
		[Tooltip("SpriteHandler used for gas color overlay")]
		public SpriteHandler gasIndicatorSpriteHandler;
	
		private GasContainer gasContainer;
		
		private readonly Color noGasColor = new(0,0,0,0);

		public override void Start()
		{
			base.Start();
			gasContainer = GetComponent<GasContainer>();
			UpdateOverlay();
		}
		
		public void OnInventoryMoveServer(InventoryMove info)
		{
			UpdateOverlay();
		}
		
		public override void Explode()
		{
			if (isServer)
			{
				UpdateTimer(false);
				gasContainer.ReleaseContentsInstantly();
				UpdateOverlay();
			}
		}
		
		private void UpdateOverlay()
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
		
		public string Examine(Vector3 pos)
		{
			return gasContainer.GasMixLocal.Pressure > 50 ? "Gas swirls around inside." : "It's empty." ;
		}
	}	
}
