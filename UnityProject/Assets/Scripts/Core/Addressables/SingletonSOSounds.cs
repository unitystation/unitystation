using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Used for getting sounds outside of  mono behaviours
/// </summary>
[CreateAssetMenu(fileName = "SingletonSOSounds", menuName = "Singleton/Addressables/SingletonSOSounds")]
public class SingletonSOSounds : SingletonScriptableObject<SingletonSOSounds>
{
   //add to ScriptableObjectSingletons

   public AddressableAudioSource ClownHonk = null;
   public AddressableAudioSource Click01 = null;
   public AddressableAudioSource Explosion1 = null;
   public AddressableAudioSource Explosion2 = null;
   public AddressableAudioSource Notice1 = null;
   public AddressableAudioSource Smash = null;
   public AddressableAudioSource Bwoink = null;
   public AddressableAudioSource Crowbar = null;
   public AddressableAudioSource screwdriver = null;
   public AddressableAudioSource WireCutter = null;
   public AddressableAudioSource Wrench = null;
   public AddressableAudioSource Weld = null;
   public AddressableAudioSource Shovel = null;
   public AddressableAudioSource Slip = null;
   public AddressableAudioSource Bodyfall = null;
   public AddressableAudioSource GenericHit = null;
   public AddressableAudioSource Wiremend = null;
   public AddressableAudioSource PunchMiss = null;
   public AddressableAudioSource ThudSwoosh = null;
   public AddressableAudioSource Sparks = null;
   public AddressableAudioSource Rustle = null;
   public AddressableAudioSource BreakStone = null;

   public AddressableAudioSource AnnouncementNotice = null;
   public AddressableAudioSource AnnouncementAnnounce = null;
   public AddressableAudioSource AnnouncementAlert = null;

   public AddressableAudioSource ShuttleDocked = null;

   public AddressableAudioSource ShuttleCalled = null;
   public AddressableAudioSource ShuttleRecalled = null;

   public AddressableAudioSource Deconstruct = null;
   public AddressableAudioSource GlassHit = null;
   public AddressableAudioSource wood3 = null;

   public AddressableAudioSource AccessDenied = null;





}
