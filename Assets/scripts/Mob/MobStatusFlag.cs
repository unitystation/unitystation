using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MobStatusFlag
{
    // See combat.dm
    // Bitflags defining which status effects could be or are inflicted on a mob
    public const int CANSTUN = 1;
    public const int CANWEAKEN = 2;
    public const int CANPARALYSE = 4;
    public const int CANPUSH = 8;
    public const int IGNORESLOWDOWN = 16;
    public const int GOTTAGOFAST = 32;
    public const int GOTTAGOREALLYFAST = 64;
    public const int GODMODE = 4096;
    public const int FAKEDEATH = 8192;	//Replaces stuff like changeling.changeling_fakedeath
    public const int DISFIGURED = 16384;	//I'll probably move this elsewhere if I ever get wround to writing a bitflag mob-damage system
    public const int XENO_HOST = 32768;	//Tracks whether we're gonna be a baby alien's mummy.
}