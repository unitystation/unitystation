using TileManagement;
using UnityEngine;

namespace Systems.Atmospherics
{
	/// <summary>
	/// Represents the potential for a MetaDataNode to ignite the gases on it, and provides logic related to igniting the actual
	/// gases.
	/// </summary>
	public class Hotspot
	{
		/// <summary>
		/// Node this hotspot lives on.
		/// </summary>
		public MetaDataNode node;

		private GameObject firelight;

		public Hotspot(MetaDataNode newNode)
		{
			node = newNode;
		}

		public void OnCreation()
		{
			//Add fire overlay
			node.ReactionManager.TileChangeManager.AddOverlay(
				node.Position, TileType.Effects, "Fire");

			//Spawn firelight prefab
			if (firelight != null) return;
			var fireLightSpawn = Spawn.ServerPrefab(node.ReactionManager.FireLightPrefab,node.Position);

			if(fireLightSpawn.Successful == false) return;
			firelight = fireLightSpawn.GameObject;
		}

		public void OnRemove()
		{
			//Remove fire overlay
			node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
				node.Position, LayerType.Effects, OverlayType.Fire);

			//Despawn firelight
			if (firelight != null)
			{
				_ = Despawn.ServerSingle(firelight);
			}
		}

		public void Process()
		{
			UpdateColour();
		}

		private void UpdateColour()
		{
			var temperature = node.GasMix.Temperature;
			var newColour = Temp2Colour(temperature);
			var alpha = 1f;

			//This is where fire is very orange, we turn it into the normal fire texture here.
			if (temperature < 5000)
			{
				//If the colour is different reset to base tile
				if (node.ReactionManager.TileChangeManager.GetColourOfFirstTile(node.Position, LayerType.Effects, OverlayType.Fire) != null)
				{
					newColour.

					//Remove fire overlay
					node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
						node.Position, LayerType.Effects, OverlayType.Fire);

					//Add fire overlay
					node.ReactionManager.TileChangeManager.AddOverlay(
						node.Position, TileType.Effects, "Fire");
				}
			}

			//Past this temperature the fire will gradually turn a bright purple
			if ()
			{

			}
		}

		/// <summary>
		/// Temperature to colour
		/// </summary>
		private static Color Temp2Colour(float temp)
		{
			// Divide by 255 to change to decimal colour
			return new Color(Temp2ColourRed(temp) / 255.0F, Temp2ColourGreen(temp) / 255.0F, Temp2ColourBlue(temp) / 255.0F);
		}

		private static float Temp2ColourRed(float temp)
		{
			temp /= 100;

			if (temp <= 66)
			{
				return 255;
			}

			return Mathf.Clamp(temp, 0, Mathf.Min(255, 329.698727446f * Mathf.Pow((temp - 60) , -0.1332047592f)));
		}

		private static float Temp2ColourGreen(float temp)
		{
			temp /= 100;

			if (temp <= 66)
			{
				return Mathf.Max(0, Mathf.Min(255, 99.4708025861f * Mathf.Log(temp) - 161.1195681661f));
			}

			return Mathf.Max(0, Mathf.Min(255, 288.1221685293f * Mathf.Pow((temp - 60) , -0.075148492f)));
		}

		private static float Temp2ColourBlue(float temp)
		{
			temp /= 100;

			if (temp <= 66)
			{
				return 255;
			}

			if (temp <= 16)
			{
				return 0;
			}

			return Mathf.Max(0, Mathf.Min(255, 138.5177312231f * Mathf.Log(temp - 10) - 305.0447927307f));
		}
	}
}