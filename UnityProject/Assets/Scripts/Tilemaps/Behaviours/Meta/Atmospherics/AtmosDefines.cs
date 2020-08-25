using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosDefines
	{
		//Plasma fire properties
		public static float OXYGEN_BURN_RATE_BASE = 1.4f;
		public static float PLASMA_BURN_RATE_DELTA = 9f;
		public static float PLASMA_MINIMUM_OXYGEN_NEEDED = 2f;
		public static float PLASMA_MINIMUM_OXYGEN_PLASMA_RATIO = 30f;
		public static float FIRE_CARBON_ENERGY_RELEASED = 100000f;	//Amount of heat released per mole of burnt carbon into the tile
		public static float FIRE_HYDROGEN_ENERGY_RELEASED = 280000f;  //Amount of heat released per mole of burnt hydrogen and/or tritium(hydrogen isotope)

		public static float FIRE_PLASMA_ENERGY_RELEASED = 3000000f;	//Amount of heat released per mole of burnt plasma into the tile
//General assmos defines.
		public static float WATER_VAPOR_FREEZE = 200f;
//freon reaction
		public static float FREON_BURN_RATE_DELTA = 4f;
		public static float FIRE_FREON_ENERGY_RELEASED = -300000f; //amount of heat absorbed per mole of burnt freon in the tile

		public static float FREON_MAXIMUM_BURN_TEMPERATURE = 293f;
		public static float FREON_LOWER_TEMPERATURE = 60f; //minimum temperature allowed for the burn to go, we would have negative pressure otherwise
		public static float FREON_OXYGEN_FULLBURN = 10f;

		public static float N2O_DECOMPOSITION_MIN_ENERGY = 1400f;
		public static float N2O_DECOMPOSITION_ENERGY_RELEASED = 200000f;

		public static float NITRYL_FORMATION_ENERGY = 100000f;
		public static float NITROUS_FORMATION_ENERGY = 10000f;
		public static float TRITIUM_BURN_OXY_FACTOR = 100f;
		public static float TRITIUM_BURN_TRIT_FACTOR = 10f;
		public static float TRITIUM_BURN_RADIOACTIVITY_FACTOR = 50000f; 	//The neutrons gotta go somewhere. Completely arbitrary number.
		public static float TRITIUM_MINIMUM_RADIATION_ENERGY = 0.1f;  	//minimum 0.01 moles trit or 10 moles oxygen to start producing rads
		public static float MINIMUM_TRIT_OXYBURN_ENERGY = 2000000f;	//This is calculated to help prevent singlecap bombs(Overpowered tritium/oxygen single tank bombs)
		public static float SUPER_SATURATION_THRESHOLD = 96f;
		public static float STIMULUM_HEAT_SCALE = 100000f;
		public static float STIMULUM_FIRST_RISE = 0.65f;
		public static float STIMULUM_FIRST_DROP = 0.065f;
		public static float STIMULUM_SECOND_RISE = 0.0009f;
		public static float STIMULUM_ABSOLUTE_DROP = 0.00000335f;
		public static float REACTION_OPPRESSION_THRESHOLD = 5f;
		public static float NOBLIUM_FORMATION_ENERGY = 2e9f; 	//1 Mole of Noblium takes the planck energy to condense.

		public static float STIM_BALL_GAS_AMOUNT = 5f;
//Research point amounts
		public static float NOBLIUM_RESEARCH_AMOUNT = 1000f;
		public static float BZ_RESEARCH_SCALE = 4f;
		public static float BZ_RESEARCH_MAX_AMOUNT = 400f;

		public static float STIMULUM_RESEARCH_AMOUNT = 50f;
//Plasma fusion properties
		public static float FUSION_ENERGY_THRESHOLD = 3e9f;	//Amount of energy it takes to start a fusion reaction
		public static float FUSION_MOLE_THRESHOLD = 250f;	//Mole count required (tritium/plasma) to start a fusion reaction
		public static float FUSION_TRITIUM_CONVERSION_COEFFICIENT = 1e-10f;
		public static float INSTABILITY_GAS_POWER_FACTOR = 0.003f;
		public static float FUSION_TRITIUM_MOLES_USED = 1f;
		public static float PLASMA_BINDING_ENERGY = 20000000f;
		public static float TOROID_VOLUME_BREAKEVEN = 1000f;
		public static float FUSION_TEMPERATURE_THRESHOLD = 10000f;
		public static float PARTICLE_CHANCE_CONSTANT = -20000000f;
		public static float FUSION_RAD_MAX = 2000f;
		public static float FUSION_RAD_COEFFICIENT = -1000f;
		public static float FUSION_INSTABILITY_ENDOTHERMALITY = 2f;
		public static float FUSION_MAXIMUM_TEMPERATURE = 1e8f;
	}
}