using System;
using UnityEngine;

namespace Objects
{
	[Serializable]
	public class Poster
	{
		public string Name;
		public string Description;
		public string Icon;
		public Sprite sprite;
		public Posters PosterName;
		public PosterType Type = PosterType.None;
	}

	public enum PosterType
	{
		None = -1,
		Official = 0,
		Contraband = 1
	}

	public enum Posters
	{
		Ripped,
		Random,
		RandomOfficial,
		RandomContraband,
		HereForYourSafety,
		NanotrasenLogo,
		Cleanliness,
		HelpOthers,
		Build,
		BlessThisSpess,
		Science,
		Ian,
		Obey,
		Walk,
		StateLaws,
		LoveIan,
		SpaceCops,
		UeNo,
		GetYourLegs,
		DoNotQuestion,
		WorkForAFuture,
		SoftCapPopArt,
		SafetyInternals,
		SafetyEyeProtection,
		SafetyReport,
		ReportCrimes,
		IonRifle,
		FoamForceAd,
		CohibaRobustoAd,
		AnniversaryVintageReprint,
		FruitBowl,
		PdaAd,
		Enlist,
		NanomichiAd,
		TwelveGauge,
		HighClassMartini,
		TheOwl,
		NoErp,
		WtfIsCo2,
		FreeTonto,
		AtmosiaIndependence,
		FunPolice,
		LustyXenomorph,
		SyndicateRecruitment,
		Clown,
		Smoke,
		GreyTide,
		MissingGloves,
		HackingGuide,
		RipBadger,
		AmbrosiaVulgaris,
		DonutCorp,
		Eat,
		Tools,
		Power,
		SpaceCube,
		CommunistState,
		Lamarr,
		BorgFancy1,
		BorgFancy2,
		Kss13,
		RebelsUnite,
		C20r,
		HaveAPuff,
		Revolver,
		DDayPromo,
		SyndicatePistol,
		EnergySwords,
		RedRum,
		CC64kAd,
		PunchShit,
		TheGriffin,
		Lizard,
		FreeDrone,
		BustyBackdoorXenoBabes6,
		RobustSoftdrinks,
		ShamblersJuice,
		PwrGame,
		SunKist,
		SpaceCola,
		SpaceUp,
		Kudzu,
		MaskedMen,
		FreeKey,
		BountyHunters,
		UnityUniteToday,
		UnityPlanet
	}
}
