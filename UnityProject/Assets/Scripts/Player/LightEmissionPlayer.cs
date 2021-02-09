using System.Collections.Generic;
using Light2D;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;


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

	[SerializeField]
	private LightSprite lightSprite = null;
	public GameObject mLightRendererObject;

	[SyncVar(hook = nameof(UpdateHook))]
	public string stringPlayerLightData;


	public void UpdateHook(string _oldString, string _stringPlayerLightData)
	{
		if (!isServer)
		{
			stringPlayerLightData = _stringPlayerLightData;
			CurrentLight = JsonUtility.FromJson<PlayerLightData>(stringPlayerLightData);
			UpdatelightSource();
		}

	}

	public bool ContainsLight(PlayerLightData Light)
	{
		return PresentLights.Contains(Light);
	}

	public void UpdateLight(PlayerLightData Light)
	{
		if (!PresentLights.Contains(Light))
		{
			return;
		}

		PresentLights.Remove(Light);
		AddLight(Light);
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
		if(CurrentLight == null){
			//CurrentLight not updated yet
			return;
		}
		if(lightSprite == null)
		{
			Logger.LogWarning("lightSprite is null (probably blank field in inspector)", Category.Lighting);
			return;
		}
		lightSprite.Color = CurrentLight.Colour;
		lightSprite.Sprite = DictionarySprites[CurrentLight.EnumSprite];
		lightSprite.Color.a = CurrentLight.Intensity;
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
