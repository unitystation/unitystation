using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum Slot {
	Music = 0,
	Announce = 1,
	FX = 2,
}

public class Synth : MonoBehaviour {
	public static Synth Instance;
//	public Text Text;
	///sampler module id 5 is hardcoded
	private static readonly int SamplerModule = 5;

	private int FxModule;

	private void Awake() {
		if ( Instance == null ) {
			Instance = this;
		} //else gets destroyed by parent
	}
	void Start() {
		Init();
//		MaryTTS.Instance.Announce( "Attention all personnel, we're fucking screwed. " +
//		                           "Syndicate ship is approaching. " +
//		                           "Central Command doesn't give a damn, so I guess we're on our own." );
		FxModule = LoadFxInstrument( "keys/fm1.sunsynth" );
	}

	public void Init() {
		try {
			int ver = SunVox.sv_init( "0", 11025, 2, 0 );//lo-fi 4ever
			if ( ver >= 0 ) {
				int major = ( ver >> 16 ) & 255;
				int minor1 = ( ver >> 8 ) & 255;
				int minor2 = ( ver ) & 255;
				log( $"SunVox lib version: {major}.{minor1}.{minor2}" );

				InitAnnounce();
				InitMusic();
				InitFX();
			} else {
				log( "sv_init() error " + ver );
			}
		} catch ( Exception e ) {
			log( "Exception: " + e );
		}
	}

	private void InitAnnounce() {
		SunVox.sv_open_slot( (int)Slot.Announce );

		log( "Loading Announce project from file..." );
//		var path = "Assets/StreamingAssets/announcement2.sunvox"; //This path is correct only for win (editor+build) and mac(editor only)
		var path = GetDataPath("announcement2.sunvox");
		if ( SunVox.sv_load( (int)Slot.Announce, path ) == 0 ) {
			log( "Loaded." );
		} else {
			log( "Load error." );
			SunVox.sv_volume( (int)Slot.Announce, 256 );
		}
	}

	private void InitMusic() {
		SunVox.sv_open_slot( (int)Slot.Music );
	}

	private void InitFX() {
		SunVox.sv_open_slot( (int)Slot.FX );
	}

	public int LoadFxInstrument( string instrument ) {
		var path = GetDataPath(instrument);
//		var path = $"Assets/StreamingAssets/{instrument}";
		int moduleId = SunVox.sv_load_module( (int)Slot.FX, path, 0, 0, 0 );
		if ( moduleId >= 0 ) {
			log( "Module loaded: " + moduleId );
			//Connect the new module to the Main Output:
			SunVox.sv_lock_slot( (int)Slot.FX );
			SunVox.sv_connect_module( (int)Slot.FX, moduleId, 0 );
			SunVox.sv_unlock_slot( (int)Slot.FX );
		} else {
			log( "Can't load the module" );
		}
		return moduleId;
	}

	public void PlayFX( int module ) {
		StartCoroutine( PlayNote( module ) );
	}

	private IEnumerator PlayNote( int module ) {
		SunVox.sv_send_event((int)Slot.FX, 0, Random.Range( 48, 72 ), 128, module + 1, 0, 0);
		yield return new WaitForSeconds( 0.1f );
		SunVox.sv_send_event((int)Slot.FX, 0, 128, 128, module + 1, 0, 0);
	}

	public void PlayMusic( string filename, byte volume = Byte.MaxValue ) {
//		var path = $"Assets/StreamingAssets/{filename}";
		var path = GetDataPath(filename);
		log( $"Loading track {filename} from {path}..." );
		if ( SunVox.sv_load( (int)Slot.Music, path ) == 0 ) {
			log( "Loaded." );
			SunVox.sv_stop( (int)Slot.Music );
			SunVox.sv_set_autostop( (int)Slot.Music, 1 );
			SunVox.sv_play_from_beginning( (int)Slot.Music );
		} else {
			log( "Load error." );
			SunVox.sv_volume( (int)Slot.Music, volume );
		}
	}

	public void PlayAnnouncement( byte[] sound ) {
		try {
			SunVox.sv_stop( (int)Slot.Announce );
			SunVox.sv_sampler_load_from_memory( (int)Slot.Announce, SamplerModule, sound, sound.Length, -1 );
			SunVox.sv_set_autostop( (int)Slot.Announce, 1 );
			SunVox.sv_play_from_beginning( (int)Slot.Announce ); //play announcement tune
			StartCoroutine( SpeakAnnouncement() );
		} catch ( Exception e ) {
			log( "Exception: " + e );
		}
	}
	private IEnumerator SpeakAnnouncement() {
		yield return new WaitForSeconds( 3f );
		SunVox.sv_send_event( (int)Slot.Announce, 0, 60, 128, SamplerModule + 1, 0, 0 );
	}

	public void StopMusic() {
		Stop( Slot.Music );
	}

	public void StopAnnouncement() {
		Stop( Slot.Announce );
	}

	public void StopFX() {
		Stop( Slot.FX );
	}

	public void StopAll() {
		foreach ( Slot slot in Enum.GetValues(typeof(Slot)) ) {
			Stop( slot );
		}
	}

	private void Stop( Slot slot ) {
		SunVox.sv_stop( (int)slot );
	}

	private string GetDataPath( string fileName ) {
		var streamingAssetsPath = "";
#if UNITY_IPHONE
    streamingAssetsPath = Application.dataPath + "/Raw";
#endif

#if UNITY_ANDROID
    streamingAssetsPath = "jar:file://" + Application.dataPath + "!/assets";
#endif

#if UNITY_STANDALONE || UNITY_EDITOR
		streamingAssetsPath = Application.streamingAssetsPath; //fixme doesn't work for mac
#endif

		var path = Path.Combine( streamingAssetsPath, fileName );
		log( "getDataPath: " + path );
		return path;
	}

	private void log( string msg ) {
		Logger.Log( msg, Category.SunVox );
	}

	private void OnDestroy() {
		if ( !enabled ) {
			return;
		}

		foreach ( Slot slot in Enum.GetValues(typeof(Slot)) ) {
			SunVox.sv_close_slot( (int)slot );
		}
		SunVox.sv_deinit();
	}

	private void OnGUI() {
		Event e = Event.current;
		if ( e.type != EventType.Used && e.isMouse && e.button == 2 )
		{
			PlayFX( FxModule );
			e.Use();
		}
	}
}