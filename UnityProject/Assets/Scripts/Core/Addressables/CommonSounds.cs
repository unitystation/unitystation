using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using ScriptableObjects;
using UnityEngine;

/// <summary>
/// Used for getting sounds outside of  mono behaviours
/// </summary>
[CreateAssetMenu(fileName = "CommonSounds", menuName = "Singleton/Addressables/CommonSounds")]
public class CommonSounds : SingletonScriptableObject<CommonSounds>
{
   //add to ScriptableObjectSingletons

   public AddressableAudioSource ClownHonk = null;
   public AddressableAudioSource Click01 = null;
   public AddressableAudioSource Explosion1 = null;
   public AddressableAudioSource Explosion2 = null;
   public AddressableAudioSource ExplosionDistant1 = null;
   public AddressableAudioSource ExplosionDistant2 = null;
   public AddressableAudioSource ExplosionCreak1 = null;
   public AddressableAudioSource ExplosionCreak2 = null;
   public AddressableAudioSource ExplosionCreak3 = null;
   public AddressableAudioSource Notice1 = null;
   public AddressableAudioSource Notice2 = null;

   public AddressableAudioSource GunEmptyAlarm = null;
   public AddressableAudioSource KineticReload = null;

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
   public AddressableAudioSource GlassBreak01 = null;

   //Ai announcements
   public AddressableAudioSource AnnouncementNotice = null;
   public AddressableAudioSource AnnouncementAnnounce = null;
   public AddressableAudioSource AnnouncementCentCom = null;
   public AddressableAudioSource AnnouncementAlert = null;
   public AddressableAudioSource AnnouncementWelcome = null;
   public AddressableAudioSource AnnouncementIntercept = null;
   public AddressableAudioSource AnnouncementCommandReport = null;

   public AddressableAudioSource ShuttleDocked = null;
   public AddressableAudioSource ShuttleCalled = null;
   public AddressableAudioSource ShuttleRecalled = null;

   public AddressableAudioSource AiMalfAnnouncement = null;
   public AddressableAudioSource AliensAnnouncement = null;
   public AddressableAudioSource Animesnnouncement = null;
   public AddressableAudioSource DisembarkAnnouncement = null;
   public AddressableAudioSource GravomaliesAnnouncement = null;
   public AddressableAudioSource HarmAlarmAnnouncement = null;
   public AddressableAudioSource IonstormAnnouncement = null;
   public AddressableAudioSource MeteorsAnnouncement = null;
   public AddressableAudioSource NewAiAnnouncement = null;
   public AddressableAudioSource Outbreak5Announcement = null;
   public AddressableAudioSource Outbreak7Announcement = null;
   public AddressableAudioSource PowerOffAnnouncement = null;
   public AddressableAudioSource PowerOnAnnouncement = null;
   public AddressableAudioSource RadiationAnnouncement = null;
   public AddressableAudioSource SpanomaliesAnnouncement = null;
   public AddressableAudioSource WelcomeAnnouncement = null;

   public AddressableAudioSource Deconstruct = null;
   public AddressableAudioSource GlassHit = null;

   public AddressableAudioSource GlassStep = null;
   public AddressableAudioSource wood3 = null;

   public AddressableAudioSource AccessDenied = null;


   public AddressableAudioSource HyperSpaceEnd;
   public AddressableAudioSource HyperSpaceBegin;
   public AddressableAudioSource HyperSpaceProgress;

   public AddressableAudioSource Tick;
   public AddressableAudioSource Tap;

   public AddressableAudioSource StealthOff;
   public AddressableAudioSource MachineHum4;
   public AddressableAudioSource Valve;

   public AddressableAudioSource WireMend;

   public AddressableAudioSource CritState;
   public AddressableAudioSource ambigen8;

   public AddressableAudioSource EatFood;

   public AddressableAudioSource PosterRipped;

   public AddressableAudioSource GlassKnock = null;

   public AddressableAudioSource ElectricShock = null;


   public AddressableAudioSource Crawl1 = null;
}
