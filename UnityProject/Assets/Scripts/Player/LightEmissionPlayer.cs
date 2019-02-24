using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.Networking;

public class LightEmissionPlayer : NetworkBehaviour
{
	public float DefaultIntensity;
	public Color DefaultColour;
	public Sprite DefaultSprite;
	public float DefaultSize;

	public PlayerLightData DefaultSettings;
	private HashSet<PlayerLightData> PresentLights = new HashSet<PlayerLightData>();
	private PlayerLightData CurrentLight = new PlayerLightData(); 

	public GameObject mLightRendererObject;


	public void AddLight(PlayerLightData Light)
	{
		if (!(PresentLights.Contains(Light)))
		{
			PresentLights.Add(Light);
			foreach (PlayerLightData Lighte in PresentLights)
			{
				if (Lighte.Intensity > CurrentLight.Intensity)
				{
					CurrentLight = Lighte;
				}
			}
			UpdatelightSource();
		}
	}
	public void RemoveLight(PlayerLightData Light)
	{
		if (PresentLights.Contains(Light))
		{
			PresentLights.Remove(Light);
			if (CurrentLight == Light)
			{
				CurrentLight = DefaultSettings;
			}
			foreach (PlayerLightData Lighte in PresentLights)
			{
				if (Lighte.Intensity > CurrentLight.Intensity)
				{
					CurrentLight = Lighte;
				}

			}
			UpdatelightSource();
		}
	}
	public void UpdatelightSource() {
			this.GetComponentInChildren<LightSprite>().Color = CurrentLight.Colour;
			this.GetComponentInChildren<LightSprite>().Sprite = CurrentLight.Sprite;
			this.GetComponentInChildren<LightSprite>().Color.a = CurrentLight.Intensity;
			mLightRendererObject.transform.localScale = new Vector3(CurrentLight.Size, CurrentLight.Size, CurrentLight.Size);
	}

	private void Awake()
	{
		DefaultSettings = new PlayerLightData()
		{
			 Intensity = DefaultIntensity,
			 Colour = DefaultColour,
			 Sprite = DefaultSprite,
			 Size = DefaultSize,
		};
		CurrentLight = DefaultSettings;
		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
		}
        UpdatelightSource();
	}
}