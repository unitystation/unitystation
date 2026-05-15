using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Shared.Managers;
using UnityEngine.AddressableAssets;
using US13.Core.Chat;
using US13.Core.Highlight;
using US13.Core.Initialisation;
using US13.Core.TranslationSystem;
using US13.Core.TTS;
using US13.Items.Implants.Organs;
using US13.Learning;
using US13.Managers;
using US13.Managers.AddressableManager;
using US13.Managers.SettingsManager;
using US13.PlayerPrefs;
using US13.UI.Core;
using US13.UI.Core.ChatBubble;
using US13.UI.Core.OptionsMenu;
using US13.UI.Core.OptionsMenu.ThemeOptions;
using US13.UI.Core.RightClick;
using US13.UI.Systems;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace DynamicOptions
{
	public class OptionManager : SingletonManager<OptionManager>
	{
		public static Dictionary<Option,OptionData> Options = new Dictionary<Option,OptionData>()
		{
			//=========================================== Display ====================================
			{Option.Fullscreen,
				new OptionData() {DisplayName = "Fullscreen", OptionType = OptionType.Abool, PreferenceKey = "Fullscreen",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {DisplaySettings.Instance.SetFullscreen((bool)o); return ""; },
					Show = () => { return true; },
					Default = () => { return false; },
				}},
			{Option.VSync,
				new OptionData() {DisplayName = "VSync", OptionType = OptionType.Abool, PreferenceKey = "VSync",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {DisplaySettings.Instance.VSyncEnabled =(bool)o; return ""; },
					Show = () => { return true; },
					Default = () => { return true; },
				}},
			{Option.TargetFPS,
				new OptionData() {DisplayName = "Target Frame Rate", OptionType = OptionType.Afloat, PreferenceKey = "TargetFPS",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => DisplayOptions.OnFrameRateTargetEdit(Mathf.RoundToInt((float)o)),
					Show = () => { return DisplaySettings.Instance.VSyncEnabled == false; },
					Default = () => { return 60f; }
				}},
			{Option.CameraZoomSize,
				new OptionData() {DisplayName = "Camera Zoom Size", OptionType = OptionType.Aslider, PreferenceKey = "CameraZoomSize",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => { DisplaySettings.Instance.ZoomLevel = Mathf.RoundToInt( (float)o * 8); return ""; },
					Show = () => { return true;},
					Default = () => { return 1f; },
					UIParameters = () => { return (new  Vector2(1f, 8f), true); },
				}},
			{Option.UIScale,
				new OptionData() {DisplayName = "UI Scale", OptionType = OptionType.AVector2, PreferenceKey = "UIScale",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {  UIManager.Instance.Scaler.referenceResolution = (Vector2) o; return ""; },
					Show = () => { return true; },
					Default = () => { return DisplaySettings.UISCALE_DEFAULT; }
				}},
			{Option.ZoomUsingScrollOption,
				new OptionData() {DisplayName = "Zoom Using Scroll Wheel", OptionType = OptionType.Abool, PreferenceKey = "ZoomUsingScrollWheel",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {  DisplaySettings.Instance.ScrollWheelZoom = (bool) o; return ""; },
					Show = () => { return true; },
					Default = () => { return true; }
				}},
			{Option.ItemLayerShadows,
				new OptionData() {DisplayName = "Item Layer Shadows", OptionType = OptionType.Abool, PreferenceKey = "ItemLayerShadows",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {  LoadManager.Instance.SetMaterialStatus((bool) o);  return ""; },
					Show = () => { return true; },
					Default = () => { return true; }
				}},
			{Option.ShuttleRadarRotation,
				new OptionData() {DisplayName = "Shuttle Radar Rotation", OptionType = OptionType.Abool, PreferenceKey = "ShuttleRadarRotation",
					OptionCategoryType = OptionCategoryType.Display,
					OnChangeAction = o => {  ShuttleCameraRenderer.RotateCamera = ((bool) o);  return ""; },
					Show = () => { return true; },
					Default = () => { return true; }
				}},
			//=========================================== Audio ====================================
			{Option.MasterVolume,
				new OptionData() {DisplayName = "MasterVolume", OptionType = OptionType.Aslider, PreferenceKey = "MasterVolume",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.MasterVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.5f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},
			{Option.MusicVolume,
				new OptionData() {DisplayName = "Music Volume", OptionType = OptionType.Aslider, PreferenceKey = "MusicVolume",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => {  AudioManager.MusicVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.2f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},
			{Option.SoundFXVolume,
				new OptionData() {DisplayName = "Sound FX Volume", OptionType = OptionType.Aslider, PreferenceKey = "SoundFXVolume",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.SoundFXVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.2f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},
			{Option.AmbientVolume,
				new OptionData() {DisplayName = "Ambient Volume", OptionType = OptionType.Aslider, PreferenceKey = "AmbientVolume",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.AmbientVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.2f; },
					UIParameters = () => { return (new Vector2(0f, 1f), false); },
				}},
			{Option.AudioReflections,
				new OptionData() {DisplayName = "Audio Reflections", OptionType = OptionType.Abool, PreferenceKey = "AudioReflections",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.Instance.EnableAudioReflections = (bool)o; return ""; },
					Show = () => { return true; },
					Default = () => { return true; },
				}},
			{Option.TTSToggle,
				new OptionData() {DisplayName = "Text to speech", OptionType = OptionType.Abool, PreferenceKey = "TTSToggle",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { UIManager.ToggleTTS((bool)o); return ""; },
					Show = () => { return true; },
					Default = () => { return true; },
				}},
			{Option.TTSVolume,
				new OptionData() {DisplayName = "Text to speech Volume", OptionType = OptionType.Aslider, PreferenceKey = "TTSVolume",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.TtsVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.2f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},
			{Option.AudioRadioNotification,
				new OptionData() {DisplayName = "Radio notification volume", OptionType = OptionType.Aslider, PreferenceKey = "AudioRadioNotification",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.RadioChatterVolume((float)o); return ""; },
					Show = () => { return true; },
					Default = () => { return 0.2f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},
			{Option.RadioCommonNotificationNoise,
				new OptionData() {DisplayName = "Enable Radio noise on common", OptionType = OptionType.Abool, PreferenceKey = "RadioCommonNotificationNoise",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => { AudioManager.CommonRadioChatter((bool)o); return ""; },
					Show = () => { return true; },
					Default = () => { return false; },
				}},
			{Option.MicVoiceChat,
				new OptionData() {DisplayName = "Microphone enabled for voice chat", OptionType = OptionType.Abool, PreferenceKey = "MicVoiceChat",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => {VoiceChatManager.ClientEnabled  = (bool)o; return ""; },
					Show = () => { return true; },
					Default = () => { return true; },
				}},
			{Option.MicPushToTalkVoiceChat,
				new OptionData() {DisplayName = "Microphone Push to Talk", OptionType = OptionType.Abool, PreferenceKey = "MicPushToTalkVoiceChat",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => {VoiceChatManager.ClientPushToTalk = (bool)o; return ""; },
					Show = () => { return true; },
					Default = () => { return false; },
				}},
			{Option.AddressableCatalogueClear,
				new OptionData() {DisplayName = "Clear Addressable Catalogue", OptionType = OptionType.AButton, PreferenceKey = "AddressableCatalogueClear",
					OptionCategoryType = OptionCategoryType.Audio,
					OnChangeAction = o => {
						Addressables.CleanBundleCache();
						Caching.ClearCache();
						AddressableCatalogueManager.LoadHostCatalogues(); return ""; },
					Show = () => { return true; },
				}},


			//===================================================== Themes =====================================================
			{Option.ChatBubbleTheme,
				new OptionData() {DisplayName = "Chat Bubble Theme", OptionType = OptionType.ADropDown, PreferenceKey = "ChatBubbleTheme",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ThemeManager.SetPreferredTheme(ThemeType.ChatBubbles, (string) o); return ""; },
					Show = () => { return true; },
					Default = () => { return "Default"; },
					UIParameters = () => { return ThemeManager.GetThemeOptions(ThemeType.ChatBubbles); },
				}},
			{Option.ChatBubbleSize,
				new OptionData() {DisplayName = "Chat Bubble Size", OptionType = OptionType.Aslider, PreferenceKey = "ChatBubbleSize",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { DisplaySettings.Instance.ChatBubbleSize = (float) o; return ""; },
					Show = () => { return true; },
					Default = () => { return 2f; },
					UIParameters = () => { return (new  Vector2(2f, 5f), true); },
				}},
			{Option.ChatBubbleCharacterSpeed,
				new OptionData() {DisplayName = "Chat Bubble Character Pop In Speed", OptionType = OptionType.Aslider, PreferenceKey = "ChatBubbleCharacterSpeed",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { DisplaySettings.Instance.ChatBubblePopInSpeed = (float) o; return ""; },
					Show = () => { return true; },
					Default = () => { return 0.05f; },
					UIParameters = () => { return (new Vector2(0f, 1f), false);},
				}},
			{Option.ChatBubbleMaxNum,
				new OptionData() {DisplayName = "Chat Bubble Maximum Number", OptionType = OptionType.Aslider, PreferenceKey = "ChatBubbleMaxNum",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ChatBubble.SetPreferenceNummberBubbles(Mathf.RoundToInt((float) o)); return ""; },
					Show = () => { return true; },
					Default = () => { return 2f; },
					UIParameters = () => { return (new  Vector2(1f, 5f), true); },
				}},
			{Option.ChatBubbleAdditionalTime,
				new OptionData() {DisplayName = "Chat Bubble Apperance Time", OptionType = OptionType.Aslider, PreferenceKey = "ChatBubbleAdditionalTime",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => {DisplaySettings.Instance.ChatBubbleAdditionalTime = (float) o; return ""; },
					Show = () => { return true; },
					Default = () => { return 2f; },
					UIParameters = () => { return (new  Vector2(0f, 10f), false); },
				}},
			{Option.ChatBubbleInstant,
				new OptionData() {DisplayName = "Instant Chat Bubbles", OptionType = OptionType.Abool, PreferenceKey = "ChatBubbleInstant",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o =>
					{
						DisplaySettings.Instance.ChatBubbleInstant = (bool) o;
						return "";
					},
					Show = () => { return true; },
					Default = () => { return false; },
				}},
			{Option.ChatBubbleClown,
				new OptionData() {DisplayName = "Colourful Clown Chat Bubbles", OptionType = OptionType.Abool, PreferenceKey = "ChatBubbleClown",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o =>
					{
						if ((bool) o)
						{
							DisplaySettings.Instance.ChatBubbleClownColour = 1;
						}
						else
						{
							DisplaySettings.Instance.ChatBubbleClownColour = 0;
						}
						return "";
					},
					Show = () => { return true; },
					Default = () => { return true; },
				}},
			{Option.ChatLogSizeOption,
				new OptionData() {DisplayName = "Chat Log Maximum Size", OptionType = OptionType.Aslider, PreferenceKey = "ChatLogSizeOption",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => {ChatUI.maxLogLength = Mathf.RoundToInt((float) o); return ""; },
					Show = () => { return true; },
					Default = () => { return 90f; },
					UIParameters = () => { return (new  Vector2(80f, 2500f), true); },
				}},
			{Option.ChatFontSize,
				new OptionData() {DisplayName = "Chat Font Size", OptionType = OptionType.Aslider, PreferenceKey = "fontscale",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { return "";  }, //the Preference does it
					Show = () => { return true; },
					Default = () => { return 1f; },
					UIParameters = () => { return (new  Vector2(1, 32), true); },
				}},
			{Option.HoverDelayOption,
				new OptionData() {DisplayName = "Hover Tooltip Delay", OptionType = OptionType.Aslider, PreferenceKey = "HoverDelayOption",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { UIManager.Instance.HoverTooltipUI.HoverDelay = (float)o; return "";  },
					Show = () => { return true; },
					Default = () => { return 0.25f; },
					UIParameters = () => { return (new  Vector2(0.1f, 2.25f), false); },
				}},

			{Option.ChatFontTheme,
				new OptionData() {DisplayName = "Chat Font", OptionType = OptionType.ADropDown, PreferenceKey = "ChatFontTheme",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ChatManager.Instance.FontIndexToUse = (string)o; return "";  },
					Show = () => { return true; },
					Default = () => { return "LiberationSans SDF"; },
					UIParameters = () => { return ChatManager.Instance.Fonts.Select(font => font.name).ToList(); },
				}},
			{Option.RightClickStyle,
				new OptionData() {DisplayName = "Right Click Style", OptionType = OptionType.ADropDown, PreferenceKey = "RightClickStyle",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { RightClickManager.SetRightClickPreference((string)o); return "";  },
					Show = () => { return true; },
					Default = () => { return nameof(PreferenceRightClickOption.Radial); },
					UIParameters = () => { return RightClickManager.AvailableRightClickOptions.Keys.ToList(); },
				}},

			{Option.EnableChatBackground,
				new OptionData() {DisplayName = "Chat Entries Always Have a Background", OptionType = OptionType.Abool, PreferenceKey = PlayerPrefKeys.CHAT_BACKGROUND_ALLWAYS_ENABLED,
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { return "";  }, //Done by preference
					Show = () => { return true; },
					Default = () => { return false; },
				}},


			{Option.ItemHighlight,
				new OptionData() {DisplayName = "Enable Item Highlight", OptionType = OptionType.Abool, PreferenceKey = "ItemHighlight",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { Highlight.SetPreference((bool)o); return "";  },
					Show = () => { return true; },
					Default = () => { return true; },
				}},

			{Option.ThrowToggle,
				new OptionData() {DisplayName = "Throw Key Hold", OptionType = OptionType.Abool, PreferenceKey = "ThrowToggle",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ControlAction.SetPreferenceThrowHoldPreference((bool)o); return "";  },
					Show = () => { return true; },
					Default = () => { return false; },
				}},

			{Option.ChatNameHighlight,
				new OptionData() {DisplayName = "Highlight Character/OOC Name In Chat", OptionType = OptionType.Abool, PreferenceKey = "ChatNameHighlight",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ThemeManager.Instance.ChatHighlightToggle((bool)o); return "";  },
					Show = () => { return true; },
					Default = () => { return true; },
				}},

			{Option.ChatMentionSound,
				new OptionData() {DisplayName = "Play Sound When Mentioned In Chat", OptionType = OptionType.Abool, PreferenceKey = "ChatMentionSound",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ThemeManager.Instance.MentionSoundToggle((bool)o); return "";  },
					Show = () => { return true; },
					Default = () => { return true; },
				}},

			{Option.MentionSoundDropDown,
				new OptionData() {DisplayName = "Mention Sound", OptionType = OptionType.ADropDown, PreferenceKey = "MentionSoundDropDown",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ThemeManager.Instance.MentionSoundIndexChange((string)o); return "";  },
					Show = () => { return true; },
					Default = () => { return "Assets/Prefabs/UI/Blip.prefab"; },
					UIParameters = () => { return ThemeManager.Instance.MentionSounds.Select(font => font.AssetAddress).ToList(); },
				}},

			{Option.MinimumChatFadeOption,
				new OptionData() {DisplayName = "Minimum background alpha for chat fade", OptionType = OptionType.Aslider, PreferenceKey = "MinimumChatFadeOption",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ChatUI.ChatMinimumBackgroundAlpha = (float)o; return "";  },
					Show = () => { return true; },
					Default = () => { return 0f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},

			{Option.MinimumChatContentFadeOption,
				new OptionData() {DisplayName = "Minimum content alpha for chat fade", OptionType = OptionType.Aslider, PreferenceKey = "MinimumChatContentFadeOption",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { ChatUI.ChatContentMinimumAlpha = (float)o; return "";  },
					Show = () => { return true; },
					Default = () => { return 0f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},


			{Option.TTSDefault,
				new OptionData() {DisplayName = "system TTS voice", OptionType = OptionType.ADropDown, PreferenceKey = "TTSDefault",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { TTSVoices.SetSystemTTS((string)o); return "";  },
					Show = () => { return true; },
					Default = () => { return "Male 01"; },
					UIParameters = () => { return TTSVoices.Voices.ToList(); },
				}},

			{Option.Language,
				new OptionData() {DisplayName = "Language", OptionType = OptionType.ADropDown, PreferenceKey = PlayerPrefKeys.LanguagePreference,
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { TranslationManager.Instance.Initialise(); return "";  },
					Show = () => { return true; },
					Default = () => { return "System"; },
					UIParameters = () => { return TranslationSystem.AvailableLanguages.ToList(); },
				}},
			{Option.ItemSwapSpeed,
				new OptionData() {DisplayName = "Item Swap Animation Speed", OptionType = OptionType.Aslider, PreferenceKey = "ItemSwapSpeed",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { UI_ItemImage.AnimationSpeed = (float)o; return "";  },
					Show = () => { return true; },
					Default = () => { return 0.15f; },
					UIParameters = () => { return (new  Vector2(0f, 1f), false); },
				}},

			{Option.MouthAnimations,
				new OptionData() {DisplayName = "Play mouth animations when speaking", OptionType = OptionType.Abool, PreferenceKey = "MouthAnimations",
					OptionCategoryType = OptionCategoryType.Theme,
					OnChangeAction = o => { Tongue.SpeechAnimationEnabled = (bool)o; return "";  },
					Show = () => { return true; },
					Default = () => { return true; },
				}},

			// ========================================= Gameplay =========================================
			{Option.PlayerExperienceLevel,
				new OptionData() {DisplayName = "Player Experience Level", OptionType = OptionType.ADropDown, PreferenceKey = "PlayerExperienceLevel",
					OptionCategoryType = OptionCategoryType.Gameplay,
					OnChangeAction = o =>
					{
						switch ((string)o)
						{
							case "New to SS13":
								ProtipManager.Instance.SetExperienceLevel(ProtipManager.ExperienceLevel.NewToSpaceStation);
								break;
							case "New to Unitystation":
								ProtipManager.Instance.SetExperienceLevel(ProtipManager.ExperienceLevel.NewToUnityStation);
								break;
							case "Experienced":
								ProtipManager.Instance.SetExperienceLevel(ProtipManager.ExperienceLevel.SomewhatExperienced);
								break;
							case "ROBUST":
								ProtipManager.Instance.SetExperienceLevel(ProtipManager.ExperienceLevel.Robust);
								break;
						}

						return "";
					},
					Show = () => { return true; },
					Default = () => { return "New to SS13"; },
					UIParameters = () => { return new List<string>(){ "New to SS13","New to Unitystation","Experienced","ROBUST" }; },
				}},
			{Option.A3DMode,
				new OptionData() {DisplayName = "A 3DMode?", OptionType = OptionType.AButton, PreferenceKey = "A3DMode",
					OptionCategoryType = OptionCategoryType.Gameplay,
					OnChangeAction = o => { Manager3D.Instance.PromptConvertTo3D(); return ""; },
					Show = () => { return true; },
				}},
			// ========================================= Misc =========================================

			{Option.StreamerMode,
				new OptionData() {DisplayName = "Streamer mode", OptionType = OptionType.Abool, PreferenceKey = "StreamerMode",
					OptionCategoryType = OptionCategoryType.Misc,
					OnChangeAction = o => {MiscSettings.Instance.StreamerModeEnabled = (bool)o; return "";  },
					Show = () => { return true; },
					Default = () => { return false; },
				}},
		};


		//how to get val, default value,
		//look up enum?
		//

		//set by UI, save by UI




		public GameObject Display;
		public GameObject Audio;
		public GameObject Theme;
		public GameObject Gameplay;
		public GameObject Misc;


		public OptionItem PrefabAbool;
		public OptionItem PrefabAfloat;
		public OptionItem PrefabAslider;
		public OptionItem PrefabAVector2;
		public OptionItem PrefabAButton;
		public OptionItem PrefabADropDown;


		public OptionCollection OptionCollectionDisplay;
		public OptionCollection OptionCollectionAudio;
		public OptionCollection OptionCollectionTheme;
		public OptionCollection OptionCollectionGameplay;
		public OptionCollection OptionCollectionMisc;

		public void Initialise()
		{
			var grouped = Options
				.GroupBy(kv => kv.Value.OptionCategoryType)
				.ToDictionary(
					g => g.Key,
					g => g.ToDictionary(kv => kv.Key, kv => kv.Value)
				);

			foreach (var KeyAndGroup in grouped)
			{
				var Parent = GetParentFromType(KeyAndGroup.Key);
				var Collection = ShowOptions(Parent, KeyAndGroup.Value);

				switch (KeyAndGroup.Key)
				{
					case OptionCategoryType.Display:
						OptionCollectionDisplay = Collection;
						break;
					case OptionCategoryType.Audio:
						OptionCollectionAudio = Collection;
						break;
					case OptionCategoryType.Gameplay:
						OptionCollectionGameplay = Collection;
						break;
					case OptionCategoryType.Theme:
						OptionCollectionTheme = Collection;
						break;
					case OptionCategoryType.Misc:
						OptionCollectionMisc = Collection;
						break;
				}
			}
		}


		public OptionItem GetPrefab( OptionType OptionType)
		{
			switch (OptionType)
			{
				case OptionType.Abool:
					return PrefabAbool;
				case OptionType.Afloat:
					return PrefabAfloat;
				case OptionType.Aslider:
					return PrefabAslider;
				case OptionType.AVector2:
					return PrefabAVector2;
				case OptionType.AButton:
					return PrefabAButton;
				case OptionType.ADropDown:
					return PrefabADropDown;
				default:
					return null;
			}

		}

		public GameObject GetParentFromType(OptionCategoryType OptionCategoryType)
		{
			switch (OptionCategoryType)
			{
				case OptionCategoryType.Display:
					return Display;
				case OptionCategoryType.Audio:
					return Audio;
				case OptionCategoryType.Theme:
					return Theme;
				case OptionCategoryType.Gameplay:
					return Gameplay;
				case OptionCategoryType.Misc:
					return Misc;
				default:
					return null;
			}
		}


		public OptionCollection ShowOptions(GameObject Parent, Dictionary<Option,OptionData> OptionDatas)
		{
			var Collection = new OptionCollection()
			{
				Parent = Parent,
				LivingOptionItems = new Dictionary<Option, OptionItem>(),
				Options = OptionDatas
			};
			foreach (var Option in OptionDatas)
			{
				try
				{
					if (Option.Value.Show.Invoke())
					{
						Collection.LivingOptionItems[Option.Key] = (InstantiateOptionItem(Option.Key, Option.Value, Parent, Collection));
					}
				}
				catch (Exception e)
				{
					Loggy.Error(e.ToString());
				}

			}

			Collection.Order();
			return Collection;
		}

		public OptionItem InstantiateOptionItem(Option Option, OptionData OptionData, GameObject Parent, OptionCollection OptionCollection)
		{
			var Prefab = GetPrefab(OptionData.OptionType);
			var Instance  = Instantiate(Prefab, Parent.transform);
			Instance.SetUp(Option,  OptionData, OptionCollection);
			return Instance;
		}

	}
	public enum Option
	{
		Fullscreen,
		VSync,
		TargetFPS,
		CameraZoomSize,
		UIScale,
		ZoomUsingScrollOption,
		ItemLayerShadows,
		ShuttleRadarRotation,
		MasterVolume,
		MusicVolume,
		SoundFXVolume,
		AmbientVolume,
		AudioReflections,
		TTSToggle,
		TTSVolume,
		AudioRadioNotification,
		RadioCommonNotificationNoise,
		MicVoiceChat,
		MicPushToTalkVoiceChat,
		AddressableCatalogueClear,
		ChatBubbleTheme,
		ChatBubbleSize,
		ChatBubbleCharacterSpeed,
		ChatBubbleMaxNum,
		ChatBubbleAdditionalTime,
		ChatBubbleInstant,
		ChatBubbleClown,
		ChatLogSizeOption,
		ChatFontSize,
		HoverDelayOption,
		ChatFontTheme,
		RightClickStyle,
		EnableChatBackground,
		ItemHighlight,
		ThrowToggle,
		ChatNameHighlight,
		ChatMentionSound,
		MentionSoundDropDown,
		MinimumChatFadeOption,
		MinimumChatContentFadeOption,
		TTSDefault,
		Language,
		PlayerExperienceLevel,
		A3DMode,
		StreamerMode,
		ItemSwapSpeed,
		MouthAnimations
	}

	public struct OptionData
	{
		public string DisplayName;
		public string PreferenceKey;
		public OptionType OptionType;
		public OptionCategoryType OptionCategoryType;
		public Func<object, string> OnChangeAction;
		public Func<bool> Show;
		public Func<object> Default;
		public Func<object> UIParameters;
	}


	public enum OptionType
	{
		Abool,
		Afloat,
		Aslider,
		AVector2,
		AButton,
		ADropDown

	}
	public enum OptionCategoryType
	{
		Display,
		Audio,
		Theme,
		Gameplay,
		Misc,
	}
}