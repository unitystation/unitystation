using System;
using System.Runtime.InteropServices;

namespace SunVox
{
	public static class SunVox {
  /*
   You can use SunVox library freely,
   but the following text should be included in your products (e.g. in About window):

   SunVox modular synthesizer
   Copyright (c) 2008 - 2017, Alexander Zolotov <nightradio@gmail.com>, WarmPlace.ru

   Ogg Vorbis 'Tremor' integer playback codec
   Copyright (c) 2002, Xiph.org Foundation
*/
  public  const int NOTECMD_NOTE_OFF = 128;
  public const int NOTECMD_ALL_NOTES_OFF = 129; /* notes of all synths off */
  public const int NOTECMD_CLEAN_SYNTHS = 130; /* stop and clean all synths */
  public const int NOTECMD_STOP = 131;
  public const int NOTECMD_PLAY = 132;

  public struct sunvox_note {
    char note; /* NN: 0 - nothing; 1..127 - note num; 128 - note off; 129, 130... - see NOTECMD_xxx defines */
    char vel; /* VV: Velocity 1..129; 0 - default */
    char module; /* MM: 0 - nothing; 1..255 - module number + 1 */
    char zero; /* ...future use... */
    short ctl; /* 0xCCEE: CC: 1..127 - controller number + 1; EE - effect */
    short ctl_val; /* 0xXXYY: value of controller or effect */
  };

  public const int SV_INIT_FLAG_NO_DEBUG_OUTPUT = (1 << 0);
  public const int SV_INIT_FLAG_USER_AUDIO_CALLBACK = (1 << 1); /* Interaction with sound card is on the user side */
  public const int SV_INIT_FLAG_AUDIO_INT16 = (1 << 2);
  public const int SV_INIT_FLAG_AUDIO_FLOAT32 = (1 << 3);
  public const int SV_INIT_FLAG_ONE_THREAD = (1 << 4); /* Audio callback and song modification functions are in single thread */

  public const int SV_MODULE_FLAG_EXISTS = 1;
  public const int SV_MODULE_FLAG_EFFECT = 2;
  public const int SV_MODULE_INPUTS_OFF = 16;
  public const int SV_MODULE_INPUTS_MASK = (255 << (byte) SV_MODULE_INPUTS_OFF);
  public const int SV_MODULE_OUTPUTS_OFF = (16 + 8);
  public const int SV_MODULE_OUTPUTS_MASK = (255 << (byte) SV_MODULE_OUTPUTS_OFF);

  public const int SV_STYPE_INT16 = 0;
  public const int SV_STYPE_INT32 = 1;
  public const int SV_STYPE_FLOAT32 = 2;
  public const int SV_STYPE_FLOAT64 = 3;

#if UNITY_EDITOR || UNITY_STANDALONE
  private const string LIBRARY_NAME = "sunvox";
#elif UNITY_IOS && !UNITY_EDITOR
  private const string LIBRARY_NAME = "__Internal";
#elif UNITY_ANDROID && !UNITY_EDITOR
  private const string LIBRARY_NAME = "libsunvox";
#endif

  /*
    Functions
    (use the functions with the label "USE LOCK/UNLOCK" within the sv_lock_slot() / sv_unlock_slot() block only!)
  */

  /*
    sv_init(), sv_deinit() - global sound system init/deinit
    Parameters:
      config - string with additional configuration in the following format: "option_name=value|option_name=value";
                example: "buffer=1024|audiodriver=alsa|audiodevice=hw:0,0";
                use null if you agree to the automatic configuration;
      freq - sample rate (Hz); min - 44100;
      channels - only 2 supported now;
      flags - mix of the SV_INIT_FLAG_xxx flags.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_init( string config, int freq, int channels, int flags );

  // Prevents sv_deinit from crashing the editor, but leaves the call alone on regular builds.
  #if UNITY_EDITOR
  public static int sv_deinit() {return 0;}
  #else
  [DllImport (LIBRARY_NAME)] public static extern int sv_deinit();
  #endif

  /*
    sv_update_input() -
    handle input ON/OFF requests to enable/disable input ports of the sound card
    (for example, after the Input module creation).
    Call it from the main thread only, where the SunVox sound stream is not locked.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_update_input();

  /*
    sv_audio_callback() - get the next piece of SunVox audio from the Output module.
    With sv_audio_callback() you can ignore the built-in SunVox sound output mechanism and use some other sound system.
    SV_INIT_FLAG_USER_AUDIO_CALLBACK flag in sv_init() mus be set.
    Parameters:
      buf - destination buffer of type signed short (if SV_INIT_FLAG_AUDIO_INT16 used in sv_init())
            or float (if SV_INIT_FLAG_AUDIO_FLOAT32 used in sv_init());
            stereo data will be interleaved in this buffer: LRLR... ; where the LR is the one frame (Left+Right channels);
      frames - number of frames in destination buffer;
      latency - audio latency (in frames);
      out_time - buffer output time (in system ticks);
    Return values: 0 - silence (buffer filled with zeroes); 1 - some signal.
    Example:
      user_out_time = ... ; //output time in user time space (NOT SunVox time space!)
      user_cur_time = ... ; //current time (user time space)
      user_ticks_per_second = ... ; //ticks per second (user time space)
      user_latency = user_out_time - use_cur_time; //latency in user time space
      unsigned int sunvox_latency = ( user_latency * sv_get_ticks_per_second() ) / user_ticks_per_second; //latency in SunVox time space
      unsigned int latency_frames = ( user_latency * sample_rate_Hz ) / user_ticks_per_second; //latency in frames
      sv_audio_callback( buf, frames, latency_frames, sv_get_ticks() + sunvox_latency );
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_audio_callback( byte[] buf, int frames, int latency, int out_time );

  /*
    sv_audio_callback2() - send some data to the Input module and receive the filtered data from the Output module.
    It's the same as sv_audio_callback() but you also can specify the input buffer.
    Parameters:
      ...
      in_type - input buffer type: 0 - signed short (16bit integer); 1 - float (32bit floating point);
      in_channels - number of input channels;
      in_buf - input buffer; stereo data will be interleaved in this buffer: LRLR... ; where the LR is the one frame (Left+Right channels);
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_audio_callback2( byte[] buf, int frames, int latency, int out_time, int in_type, int in_channels, byte[] in_buf );

  /*
    sv_open_slot(), sv_close_slot(), sv_lock_slot(), sv_unlock_slot() -
    open/close/lock/unlock sound slot for SunVox.
    You can use several slots simultaneously (each slot with its own SunVox engine)
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_open_slot( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_close_slot( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_lock_slot( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_unlock_slot( int slot );

  /*
    sv_load(), sv_load_from_memory() -
    load SunVox project from the file or from the memory block.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_load( int slot, string name );
  [DllImport (LIBRARY_NAME)] public static extern int sv_load_from_memory( int slot, byte[] data, int data_size );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_play( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_play_from_beginning( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_stop( int slot );

  /*
    sv_set_autostop()
    autostop values: 0 - disable autostop; 1 - enable autostop.
    When disabled, song is playing infinitely in the loop.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_set_autostop( int slot, int autostop );

  /*
    sv_end_of_song() return values: 0 - song is playing now; 1 - stopped.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_end_of_song( int slot );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_rewind( int slot, int line_num );

  /*
    sv_volume() - set volume from 0 (min) to 256 (max 100%)
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_volume( int slot, int vol );

  /*
    sv_send_event() - send some event (note ON, note OFF, controller change, etc.)
    Parameters:
      slot;
      track_num - track number within the pattern;
      note: 0 - nothing; 1..127 - note num; 128 - note off; 129, 130... - see NOTECMD_xxx defines;
      vel: velocity 1..129; 0 - default;
      module: 0 - nothing; 1..255 - module number + 1;
      ctl: 0xCCEE. CC - number of a controller (1..255). EE - effect;
      ctl_val: value of controller or effect.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_send_event( int slot, int track_num, int note, int vel, int module, int ctl, int ctl_val );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_current_line( int slot ); /* Get current line number */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_current_line2( int slot ); /* Get current line number in fixed point format 27.5 */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_current_signal_level( int slot, int channel ); /* From 0 to 255 */
  [DllImport (LIBRARY_NAME)] public static extern IntPtr sv_get_song_name( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_song_bpm( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_song_tpl( int slot );

  /*
    sv_get_song_length_frames(), sv_get_song_length_lines() -
    get the project length.
    Frame is one discrete of the sound. Sample rate 44100 Hz means, that you hear 44100 frames per second.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_song_length_frames( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_song_length_lines( int slot );

  /*
    sv_new_module() - create a new module;
    sv_remove_module() - remove selected module;
    sv_connect_module() - connect the source to the destination;
    sv_disconnect_module() - disconnect the source from the destination;
    sv_load_module() - load a module or sample; supported file formats: sunsynth, xi, wav, aiff;
                        return value: new module number or negative value in case of some error;
    sv_load_module_from_memory() - load a module or sample from the memory block;
    sv_sampler_load() - load a sample to already created Sampler; to replace the whole sampler - set sample_slot to -1;
    sv_sampler_load_from_memory() - load a sample from the memory block;
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_new_module( int slot, string type, string name, int x, int y, int z ); /* USE LOCK/UNLOCK! */
  [DllImport (LIBRARY_NAME)] public static extern int sv_remove_module( int slot, int mod_num ); /* USE LOCK/UNLOCK! */
  [DllImport (LIBRARY_NAME)] public static extern int sv_connect_module( int slot, int source, int destination ); /* USE LOCK/UNLOCK! */
  [DllImport (LIBRARY_NAME)] public static extern int sv_disconnect_module( int slot, int source, int destination ); /* USE LOCK/UNLOCK! */
  [DllImport (LIBRARY_NAME)] public static extern int sv_load_module( int slot, string file_name, int x, int y, int z );
  [DllImport (LIBRARY_NAME)] public static extern int sv_load_module_from_memory( int slot, byte[] data, int data_size, int x, int y, int z );
  [DllImport (LIBRARY_NAME)] public static extern int sv_sampler_load( int slot, int sampler_module, string file_name, int sample_slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_sampler_load_from_memory( int slot, int sampler_module, byte[] data, int data_size, int sample_slot );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_number_of_modules( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_module_flags( int slot, int mod_num ); /* SV_MODULE_FLAG_xxx */

  /*
    sv_get_module_inputs(), sv_get_module_outputs() -
    get pointers to the int[] arrays with the input/output links.
    Number of inputs = ( module_flags & SV_MODULE_INPUTS_MASK ) >> SV_MODULE_INPUTS_OFF.
    Number of outputs = ( module_flags & SV_MODULE_OUTPUTS_MASK ) >> SV_MODULE_OUTPUTS_OFF.
  */
  [DllImport (LIBRARY_NAME)] public static extern int[] sv_get_module_inputs( int slot, int mod_num );
  [DllImport (LIBRARY_NAME)] public static extern int[] sv_get_module_outputs( int slot, int mod_num );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern IntPtr sv_get_module_name( int slot, int mod_num );

  /*
    sv_get_module_xy() - get module XY coordinates packed in a single uint32 value:
    ( x & 0xFFFF ) | ( ( y & 0xFFFF ) << 16 ).
    Normal working area: 0x0 ... 1024x1024
    Center: 512x512
    Use SV_GET_MODULE_XY() macro to unpack X and Y.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_module_xy( int slot, int mod_num );

  /*
    sv_get_module_color() - get module color in the following format: 0xBBGGRR
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_module_color( int slot, int mod_num );

  /*
    sv_get_module_scope2() return value = received number of samples (may be less or equal to samples_to_read).
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_module_scope2( int slot, int mod_num, int channel, short[] dest_buf, int samples_to_read );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_number_of_module_ctls( int slot, int mod_num );
  [DllImport (LIBRARY_NAME)] public static extern IntPtr sv_get_module_ctl_name( int slot, int mod_num, int ctl_num );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_module_ctl_value( int slot, int mod_num, int ctl_num, int scaled );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_number_of_patterns( int slot );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_pattern_x( int slot, int pat_num );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_pattern_y( int slot, int pat_num );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_pattern_tracks( int slot, int pat_num );
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_pattern_lines( int slot, int pat_num );

  /*
    sv_get_pattern_data() - get the pattern buffer (for reading and writing)
    containing notes (events) in the following order:
      line 0: note for track 0, note for track 1, ... note for track X;
      line 1: note for track 0, note for track 1, ... note for track X;
      ...
      line X: ...
    Example:
      int pat_tracks = sv_get_pattern_tracks( slot, pat_num ); //number of tracks
      sunvox_note* data = sv_get_pattern_data( slot, pat_num ); //get the buffer with all the pattern events (notes)
      sunvox_note* n = &data[ line_number * pat_tracks + track_number ];
      ... and then do someting with note n ...
  */
  [DllImport (LIBRARY_NAME)] public static extern sunvox_note[] sv_get_pattern_data( int slot, int pat_num );

  /*
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_pattern_mute( int slot, int pat_num, int mute ); /* USE LOCK/UNLOCK! */

  /*
    SunVox engine uses its own time space, measured in system ticks (don't confuse it with the project ticks);
    required when calculating the out_time parameter in the sv_audio_callback().
    Use sv_get_ticks() to get current tick counter (from 0 to 0xFFFFFFFF).
    Use sv_get_ticks_per_second() to get the number of SunVox ticks per second.
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_ticks();
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_ticks_per_second();

  /*
    sv_get_log() - get the latest messages from the log
    Parameters:
      size - max number of bytes to read.
    Return value: pointer to the null-terminated string with the latest log messages.
  */
  [DllImport (LIBRARY_NAME)] public static extern string sv_get_log( int size );

  /*
    DEPRECATED FUNCTIONS
  */
  [DllImport (LIBRARY_NAME)] public static extern int sv_get_sample_type(); /* Get internal sample type of the SunVox engine. Return value: one of the SV_STYPE_xxx defines. Use it to get the scope buffer type from get_module_scope() function. */
  [DllImport (LIBRARY_NAME)] public static extern byte[] sv_get_module_scope( int slot, int mod_num, int channel, int[] buffer_offset, int[] buffer_size ); /* Use sv_get_module_scope2() */
}
}

