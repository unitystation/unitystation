using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;

// Note: Judging the "lighting" sprite sheet it seems that light source can have many disabled states.
// At this point i just want to do a basic setup for an obvious extension, so only On / Off states are actually implemented
// and for other states is just a state and sprite assignment.
internal enum LightState
{
	None = 0,

	On,
	Off,

	// Placeholder states, i assume naming would change.
	MissingBulb,
	Dirty,
	Broken,

	TypeCount,
}
/// <summary>
/// Light source, such as a light bar. Note that for wall protrusion lights such as light tubes / light bars,
/// LightSwitch automatically sets their RelatedAPC if it's looking in their general direction
/// </summary>
[ExecuteInEditMode]
public class LightSource : ObjectTrigger
{
	private const LightState InitialState = LightState.Off;

	private readonly Dictionary<LightState, Sprite> mSpriteDictionary = new Dictionary<LightState, Sprite>((int)LightState.TypeCount);

	[Header("Generates itself if this is null:")]
	public GameObject mLightRendererObject;
	private LightState mState;
	private SpriteRenderer Renderer;
	private float fullIntensityVoltage = 240;
	public float Resistance = 1200;
	private bool tempStateCache;
	private float _intensity;
	/// <summary>
	/// Current intensity of the lights, automatically clamps and updates sprites when set
	/// </summary>
	private float Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			value = Mathf.Clamp(value, 0, 1);
			if (_intensity != value)
			{
				_intensity = value;
				OnIntensityChange();
			}
		}
	}

	///Note that for wall protrusion lights such as light tubes / light bars,
	// LightSwitch automatically sets this if it's looking in their general direction
	public APC RelatedAPC;
	public LightSwitch relatedLightSwitch;
	public Color customColor; //Leave null if you want default light color.

	// For network sync reliability.
	private bool waitToCheckState;

	private LightState State
	{
		get
		{
			return mState;
		}

		set
		{
			if (mState == value)
				return;

			mState = value;

			OnStateChange(value);
		}
	}
	public override void Trigger(bool iState)
	{
		// Leo Note: Some sync magic happening here. Decided not to touch it.
		tempStateCache = iState;

		if (waitToCheckState)
		{
			return;
		}

		if (Renderer == null)
		{
			waitToCheckState = true;
			if ( this != null )
			{
				StartCoroutine(WaitToTryAgain());
			}
			return;
		}
		else
		{
			State = iState ? LightState.On : LightState.Off;
		}
	}

	//this is the method broadcast invoked by LightSwitch to tell this light source what switch is driving it.
	//it is buggy and unreliable especially when client joins on a rotated matrix, client and server do not agree
	//on which switch owns which light
	public void Received(LightSwitchData Received)
	{
		//Logger.Log (Received.LightSwitchTrigger.ToString() + " < LightSwitchTrigger" + Received.RelatedAPC.ToString() + " < APC" + Received.state.ToString() + " < state" );
		tempStateCache = Received.state;

		if (waitToCheckState)
		{
			return;
		}
		if (Received.LightSwitch == relatedLightSwitch || relatedLightSwitch == null)
		{
			if (relatedLightSwitch == null)
			{
				relatedLightSwitch = Received.LightSwitch;
			}
			if (Received.RelatedAPC != null)
			{
				RelatedAPC = Received.RelatedAPC;
				{
					if (State == LightState.On)
					{
						if (!RelatedAPC.ConnectedSwitchesAndLights[relatedLightSwitch].Contains(this))
						{
							RelatedAPC.ConnectedSwitchesAndLights[relatedLightSwitch].Add(this);
						}

					}
				}
			}
			else if (relatedLightSwitch.SelfPowered)
			{
				if (State == LightState.On)
				{
					if (!relatedLightSwitch.SelfPowerLights.Contains(this))
					{
						relatedLightSwitch.SelfPowerLights.Add(this);
					}

				}
			}

			if (Renderer == null)
			{
				waitToCheckState = true;
				StartCoroutine(WaitToTryAgain());
				return;
			}
			else
			{
				State = Received.state ? LightState.On : LightState.Off;
			}
		}


	}

	//broadcast target - invoked so lights can register themselves with this APC in
	//LightSwitch.DetectLightsAndAction. There must be a better way to do this that doesn't rely
	//on broadcasting.
	public void EmergencyLight(LightSwitchData Received)
	{
		if (gameObject.tag == "EmergencyLight")
		{
			var emergLightAnim = gameObject.GetComponent<EmergencyLightAnimator>();
			if (emergLightAnim != null)
			{
				Received.RelatedAPC.ConnectEmergencyLight(emergLightAnim);

			}
		}

	}
	private void OnIntensityChange()
	{
		//we were getting an NRE here internally in GetComponent so this checks if the object lifetime
		//is up according to Unity
		if (this == null) return;
		var lightSprites = GetComponentInChildren<LightSprite>();
		if (lightSprites)
		{
			lightSprites.Color.a = Intensity;
		}
	}
	private void OnStateChange(LightState iValue)
	{
		// Assign state appropriate sprite to the LightSourceObject.
		if (mSpriteDictionary.ContainsKey(iValue))
		{
			Renderer.sprite = mSpriteDictionary[iValue];
		}
		else if (mSpriteDictionary.Any())
		{
			Renderer.sprite = mSpriteDictionary.Values.First();
		}

		// Switch Light renderer.
		if (mLightRendererObject != null)
			mLightRendererObject.SetActive(iValue == LightState.On);
	}

	public void PowerLightIntensityUpdate(float Voltage)
	{
		if (State == LightState.Off)
		{
			//RelatedAPC.ListOfLights.Remove(this);
			//RelatedAPC = null;
		}
		else
		{
			// Intensity clamped between 0 and 1, and sprite updated automatically with custom get set
			Intensity = Voltage / fullIntensityVoltage;
		}
	}

	private void Awake()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		Renderer = GetComponentInChildren<SpriteRenderer>();

		if (mLightRendererObject == null)
		{
			mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
		}

		State = InitialState;

		ExtractLightSprites();

		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab("GlassShard", gameObject.TileWorldPosition().To3Int(), transform.parent, count: 2,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
	}

	#if UNITY_EDITOR
	void Update()
	{
		if (!Application.isPlaying)
		{
			if (gameObject.tag == "EmergencyLight")
			{
				if (RelatedAPC == null)
				{

					Logger.LogError("EmergencyLight is missing APC reference, at " + transform.position, Category.Electrical);
					RelatedAPC.Current = 1; //so It will bring up an error, you can go to click on to go to the actual object with the missing reference
				}
			}
			return;
		}
	}
	#endif

	void Start()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		Color _color;

		if (customColor == new Color(0, 0, 0, 0))
		{
			_color = new Color(0.7264151f, 0.7264151f, 0.7264151f, 0.8f);
		}
		else
		{
			_color = customColor;
		}

		mLightRendererObject.GetComponent<LightSprite>().Color = _color;
	}

	private void ExtractLightSprites()
	{
		// Reimplementation of sprite location on atlas.

		// Note: It is quite magical and really should not be done like this:
		// It takes an assigned sprite name, parses its index, adds 4 to it and takes resulting sprite from the sheet.
		// There is a bold assumption that sprite sheets associated with states are spaced 4 indexes between, and that nobody has changed any sprite names.
		// My reimplementation just grabs more sprites for associated states.

		var _assignedSprite = Renderer.sprite;

		if (_assignedSprite == null)
		{
			Logger.LogError("LightSource: Unable to extract light source state sprites from SpriteSheet. Operation requires Renderer.sprite to be assigned in inspector.", Category.Lighting);
			return;
		}

		// Try to parse base sprite index.
		string[] _splitedName = _assignedSprite.name.Split('_');

		if (_splitedName.Length == 2 && int.TryParse(_splitedName[1], out _))
		{
			mSpriteDictionary.Add(LightState.On, _assignedSprite);


			/* these don't work as expected - _spriteSheet always is an empty array

			Func<int, Sprite> ExtractSprite = delegate (int iIndex)
			{
				if (iIndex >= 0 && iIndex < _spriteSheet.Length)
					return _spriteSheet[iIndex];

				return null;
			};

			// Extract sprites from sprite sheet based on spacing from base index.
			mSpriteDictionary.Add(LightState.Off, ExtractSprite(_baseIndex + SheetSpacing));
			mSpriteDictionary.Add(LightState.MissingBulb, ExtractSprite(_baseIndex + (SheetSpacing * 2)));
			mSpriteDictionary.Add(LightState.Dirty, ExtractSprite(_baseIndex + (SheetSpacing * 3)));
			mSpriteDictionary.Add(LightState.Broken, ExtractSprite(_baseIndex + (SheetSpacing * 4)));
			*/
		}
		else
		{
			mSpriteDictionary.Add(LightState.On, _assignedSprite);
		}
	}

	// Handle sync failure.
	private IEnumerator WaitToTryAgain()
	{
		yield return WaitFor.Seconds(0.2f);
		if (Renderer == null)
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
			if (Renderer != null)
			{
				State = tempStateCache ? LightState.On : LightState.Off;
				if (mLightRendererObject != null)
				{
					mLightRendererObject.SetActive(tempStateCache);
				}
			}
			else
			{
				Logger.LogWarning("LightSource still failing Renderer sync", Category.Lighting);
			}
		}
		else
		{
			State = tempStateCache ? LightState.On : LightState.Off;
			if (mLightRendererObject != null)
			{
				mLightRendererObject.SetActive(tempStateCache);
			}
		}
		waitToCheckState = false;
	}
}