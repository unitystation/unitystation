using System;

/// <summary>
/// Defines what things an object resists or is susceptible to.
/// </summary>
[Serializable]
public class Resistances
{
	//immune to lava damage
	public bool LavaProof;
	//immune to fire damage (but not necessarily lava or heat)
	public bool FireProof;
	//can catch on fire
	public bool Flammable;
	//acid can't even appear on it or melt it
	public bool UnAcidable;
	//acid can get on it but not melt it
	public bool AcidProof;
	//can't take damage
	public bool Indestructable;
	//can't be frozen
	public bool FreezeProof;
}