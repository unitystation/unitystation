using Core.Lighting_System.Light2D;
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

		//The fire light on this hotspot's tile
		private NetworkLight firelight;

		//Overlay stuff
		private bool hasSparkle;
		private bool hasOvercharge;
		private bool hasFusion;

		private uint timer;

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
			firelight = fireLightSpawn.GameObject.GetComponent<NetworkLight>();
		}

		public void OnRemove()
		{
			//Remove fire overlays
			node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
				node.Position, LayerType.Effects, OverlayType.Fire);

			if (hasSparkle)
			{
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireSparkles);
			}

			if (hasOvercharge)
			{
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireOverCharged);
			}

			if (hasFusion)
			{
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireFusion);

				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireRainbow);
			}

			//Despawn firelight
			if (firelight != null)
			{
				_ = Despawn.ServerSingle(firelight.gameObject);
			}
		}

		public void Process()
		{
			timer++;
			UpdateColour();
		}

		private void UpdateColour()
		{
			if(timer < 7) return;
			timer = 0;

			if(firelight == null) return;

			var temperature = node.GasMix.Temperature;
			var temp2Colour = Temp2Colour(temperature);
			var heatR = temp2Colour.r * 255;
			var heatG = temp2Colour.g * 255;
			var heatB = temp2Colour.b * 255;
			var heatA = 255f;

			var greyscaleFire = 1f; //This determines how greyscaled the fire is.

			//This is where fire is very orange, we turn it into the normal fire texture here.
			if (temperature < 5000)
			{
				var normalAmt = DMMath.GaussLerp(temperature, 1000, 3000);
				heatR = DMMath.Lerp(heatR, 255, normalAmt);
				heatG = DMMath.Lerp(heatG, 255, normalAmt);
				heatB = DMMath.Lerp(heatB, 255, normalAmt);
				heatA -= DMMath.GaussLerp(temperature, -5000, 5000) * 128;
				greyscaleFire -= normalAmt;
			}

			//Past this temperature the fire will gradually turn a bright purple
			if (temperature > 40000)
			{
				var purpleAmt = temperature <  DMMath.Lerp(40000, 200000, 0.5f) ? DMMath.GaussLerp(temperature, 40000, 200000) : 1;
				heatR = DMMath.Lerp(heatR, 255, purpleAmt);
			}

			//Somewhere at this temperature nitryl happens.
			if (temperature > 200000 && temperature < 500000)
			{
				var sparkleAmt = DMMath.GaussLerp(temperature, 200000, 500000);
				var newColour = new Color(1f, 1f, 1f, sparkleAmt);

				var currentColour = node.ReactionManager.TileChangeManager.GetColourOfFirstTile(node.Position,
					OverlayType.FireSparkles, LayerType.Effects);

				//Only add/remove if we need to
				if (currentColour != newColour)
				{
					if (currentColour != null && hasSparkle)
					{
						//Remove old so it can be replaced by one with different alpha value
						node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
							node.Position, LayerType.Effects, OverlayType.FireSparkles);
					}

					hasSparkle = true;

					//Add new
					node.ReactionManager.TileChangeManager.AddOverlay(
						node.Position, TileType.Effects, "FireSparkles", color: newColour);
				}
			}
			else if (hasSparkle)
			{
				hasSparkle = false;

				//Remove as its not needed anymore
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireSparkles);
			}

			//Lightning because very anime.
			if (temperature > 400000 && temperature < 1500000)
			{
				if (hasOvercharge == false)
				{
					hasOvercharge = true;

					//Add new
					node.ReactionManager.TileChangeManager.AddOverlay(
						node.Position, TileType.Effects, "FireOverCharged");
				}
			}
			else if (hasOvercharge)
			{
				hasOvercharge = false;

				//Remove overcharge as its not needed anymore
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireOverCharged);
			}

			//This is where noblium happens. Some fusion-y effects.
			if (temperature > 4500000)
			{
				var fusionAmt = temperature < DMMath.Lerp(4500000, 12000000, 0.5f)
					? DMMath.GaussLerp(temperature, 4500000, 12000000) : 1;
				var newColour = new Color(1f, 1f, 1f, fusionAmt);

				var currentColour = node.ReactionManager.TileChangeManager.GetColourOfFirstTile(node.Position,
					OverlayType.FireSparkles, LayerType.Effects);

				//Only add/remove if we need to
				if (currentColour != newColour)
				{
					if (currentColour != null && hasFusion)
					{
						//Remove old so it can be replaced by one with different alpha value
						node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
							node.Position, LayerType.Effects, OverlayType.FireFusion);

						node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
							node.Position, LayerType.Effects, OverlayType.FireRainbow);
					}

					hasFusion = true;

					//Add new
					node.ReactionManager.TileChangeManager.AddOverlay(
						node.Position, TileType.Effects, "FireFusion", color: newColour);

					//Add new
					node.ReactionManager.TileChangeManager.AddOverlay(
						node.Position, TileType.Effects, "FireRainbow", color: newColour);

					heatR = DMMath.Lerp(heatR, 255, fusionAmt);
					heatG = DMMath.Lerp(heatG, 255, fusionAmt);
					heatB = DMMath.Lerp(heatB, 255, fusionAmt);
				}

			}
			else if (hasFusion)
			{
				hasFusion = false;

				//Remove fusion and rainbow as they are not needed anymore
				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireFusion);

				node.ReactionManager.TileChangeManager.RemoveOverlaysOfType(
					node.Position, LayerType.Effects, OverlayType.FireRainbow);
			}

			firelight.SetColour(new Color(
				DMMath.Lerp(250, heatR, greyscaleFire) / 255,
				DMMath.Lerp(250, heatG, greyscaleFire) / 255,
				DMMath.Lerp(250, heatB, greyscaleFire) / 255,
				heatA / 255));
		}

		#region Colour

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

		#endregion
	}
}