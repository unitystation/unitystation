using System.Collections.Generic;
using Light2D;
using UnityEngine;
using UnityEngine.Networking;


public class LightEmissionPlayer : NetworkBehaviour
{
	public float DefaultIntensity;
	public Color DefaultColour;
	public EnumSpriteLightData DefaultEnumSprite;
	//public Sprite DefaultSprite;
	public float DefaultSize;

	public List<Sprite> Sprites = new List<Sprite>();
	public List<EnumSpriteLightData> NameSprite = new List<EnumSpriteLightData>();
	public Dictionary<EnumSpriteLightData, Sprite> DictionarySprites = new Dictionary<EnumSpriteLightData, Sprite>();

	public PlayerLightData DefaultSettings;
	private HashSet<PlayerLightData> PresentLights = new HashSet<PlayerLightData>();
	private PlayerLightData CurrentLight = new PlayerLightData();

	public GameObject mLightRendererObject;

	[SyncVar(hook = "UpdateHook")]
	public string stringPlayerLightData;


	public void UpdateHook(string _stringPlayerLightData)
	{
		if (!isServer)
		{
			stringPlayerLightData = _stringPlayerLightData;
			CurrentLight = JsonUtility.FromJson<PlayerLightData>(stringPlayerLightData);
			UpdatelightSource();
		}

	}

	public void AddLight(PlayerLightData Light)
	{
		if (!(PresentLights.Contains(Light)))
		{
			PresentLights.Add(Light);
			foreach (PlayerLightData Lighte in PresentLights) //The light that is worked out to be in charges chosen by the one that's has the highest intensity
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
	public void UpdatelightSource()
	{
		this.GetComponentInChildren<LightSprite>().Color = CurrentLight.Colour;
		this.GetComponentInChildren<LightSprite>().Sprite = DictionarySprites[CurrentLight.EnumSprite];
		this.GetComponentInChildren<LightSprite>().Color.a = CurrentLight.Intensity;
		mLightRendererObject.transform.localScale = new Vector3(CurrentLight.Size, CurrentLight.Size, CurrentLight.Size);
		if (isServer)
		{
			//Sends off to client the current Light
			stringPlayerLightData = JsonUtility.ToJson(CurrentLight);
		}
	}

	private void Awake()
	{
		for (int i = 0; i < Sprites.Count; i++)
		{
			DictionarySprites[NameSprite[i]] = Sprites[i];
		}
		DefaultSettings = new PlayerLightData()
		{
			Intensity = DefaultIntensity,
			Colour = DefaultColour,
			//Sprite = DefaultSprite,
			EnumSprite = DefaultEnumSprite,
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