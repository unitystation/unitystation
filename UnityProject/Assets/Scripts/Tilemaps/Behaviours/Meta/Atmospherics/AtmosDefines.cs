using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class AtmosDefines
	{
		public const float MINIMUM_MOLE_COUNT	= 0.01f;
		public const float FIRE_MINIMUM_TEMPERATURE_TO_EXIST	= 373.15f;

		//Conductivity
		public const float MINIMUM_TEMPERATURE_DELTA_TO_CONSIDER	= 0.5f;
		public const float MINIMUM_TEMPERATURE_FOR_SUPERCONDUCTION	= 373.15f;
		public const float MINIMUM_TEMPERATURE_START_SUPERCONDUCTION	= 693.15f;

		public const float M_CELL_WITH_RATIO	= MOLES_CELLSTANDARD * 0.005f;

		//moles in a 2.5 m^3 cell at 101.325 Pa and 20 degC (103 or so)
		public const float MOLES_CELLSTANDARD	= ONE_ATMOSPHERE * CELL_VOLUME / (293.15f * Gas.R);
		public const float ONE_ATMOSPHERE	= 101.325f;

		//liters in a cell
		public const float CELL_VOLUME	= 2500f;

		public const float SPACE_TEMPERATURE	= 2.7f;
		public const float SPACE_HEAT_CAPACITY	= 700000f;
		public const float SPACE_THERMAL_CONDUCTIVITY	= 0.4f;

		//Plasma fire properties
		public const float OXYGEN_BURN_RATE_BASE = 1.5f;
		public const float PLASMA_BURN_RATE_DELTA = 13f;
		public const float PLASMA_MINIMUM_OXYGEN_NEEDED = 2f;
		public const float PLASMA_MINIMUM_OXYGEN_PLASMA_RATIO = 30f;
		public const float PLASMA_UPPER_TEMPERATURE = 1643.15f;
		public const float PLASMA_MINIMUM_BURN_TEMPERATURE = 373.15f;
		public const int PLASMA_OXYGEN_FULLBURN = 10;
		public const float FIRE_CARBON_ENERGY_RELEASED = 100000f;	//Amount of heat released per mole of burnt carbon into the tile
		public const float FIRE_HYDROGEN_ENERGY_RELEASED = 2800000f;  //Amount of heat released per mole of burnt hydrogen and/or tritium(hydrogen isotope)
		public const float FIRE_HYDROGEN_ENERGY_WEAK = 280000f;

		public const float FIRE_PLASMA_ENERGY_RELEASED = 3000000f;	//Amount of heat released per mole of burnt plasma into the tile
//General assmos defines.
		public const float WATER_VAPOR_FREEZE = 200f;
//freon reaction
		public const float FREON_BURN_RATE_DELTA = 4f;
		public const float FIRE_FREON_ENERGY_RELEASED = -300000f; //amount of heat absorbed per mole of burnt freon in the tile

		public const float FREON_MAXIMUM_BURN_TEMPERATURE = 293f;
		public const float FREON_LOWER_TEMPERATURE = 60f; //minimum temperature allowed for the burn to go, we would have negative pressure otherwise
		public const float FREON_OXYGEN_FULLBURN = 10f;

		public const float N2O_DECOMPOSITION_MIN_ENERGY = 1400f;
		public const float N2O_DECOMPOSITION_ENERGY_RELEASED = 200000f;

		public const float NITRYL_FORMATION_ENERGY = 100000f;
		public const float NITROUS_FORMATION_ENERGY = 10000f;
		public const float TRITIUM_BURN_OXY_FACTOR = 200f;
		public const float TRITIUM_BURN_TRIT_FACTOR = 5f;
		public const float TRITIUM_BURN_RADIOACTIVITY_FACTOR = 50000f; 	//The neutrons gotta go somewhere. Completely arbitrary number.
		public const float TRITIUM_MINIMUM_RADIATION_ENERGY = 0.1f;  	//minimum 0.01 moles trit or 10 moles oxygen to start producing rads
		public const float MINIMUM_TRIT_OXYBURN_ENERGY = 2000000f;	//This is calculated to help prevent singlecap bombs(Overpowered tritium/oxygen single tank bombs)
		public const float SUPER_SATURATION_THRESHOLD = 96f;
		public const float STIMULUM_HEAT_SCALE = 100000f;
		public const float STIMULUM_FIRST_RISE = 0.65f;
		public const float STIMULUM_FIRST_DROP = 0.065f;
		public const float STIMULUM_SECOND_RISE = 0.0009f;
		public const float STIMULUM_ABSOLUTE_DROP = 0.00000335f;
		public const float REACTION_OPPRESSION_THRESHOLD = 5f;
		public const float NOBLIUM_FORMATION_ENERGY = 2e9f; 	//1 Mole of Noblium takes the planck energy to condense.
		public const float STIM_BALL_GAS_AMOUNT = 5f;

		//Hydrogen reactions
		public const float HYRDOGEN_MIN_CRYSTALLISE_TEMPERATURE = 1273f; //The minimum temperature needed for hydrogen to crystallise
		public const float HYRDOGEN_MAX_CRYSTALLISE_TEMPERATURE = 10273f; //The maximum temperature needed for hydrogen to crystallise
		public const float HYRDOGEN_CRYSTALLISE_ENERGY = 50000f; //Amount of energy it takes to crystallise hydrogen (per mol of H2)
		public const float HYDROGEN_CRYSTALLISE_RATE = 2f;

		//Hydrogen formation is based off of the process of steam reforming. For the purpose of unitystation, plasma is assumed to be methane for this reaction. Reaction is endothermic.
		public const float HYDROGEN_FORM_MIN_TEMPERATURE = 473f;
		public const float HYDROGEN_FORMATION_ENERGY = 206000f;

		//Research point amounts
		public const float NOBLIUM_RESEARCH_AMOUNT = 1000f;
		public const float BZ_RESEARCH_SCALE = 4f;
		public const float BZ_RESEARCH_MAX_AMOUNT = 400f;
		public const float STIMULUM_RESEARCH_AMOUNT = 50f;

		//Plasma fusion properties
		public const float FUSION_ENERGY_THRESHOLD = 3e9f;	//Amount of energy it takes to start a fusion reaction
		public const float FUSION_MOLE_THRESHOLD = 250f;	//Mole count required (tritium/plasma) to start a fusion reaction
		public const float FUSION_TRITIUM_CONVERSION_COEFFICIENT = 1e-10f;
		public const float INSTABILITY_GAS_POWER_FACTOR = 0.003f;
		public const float FUSION_TRITIUM_MOLES_USED = 1f;
		public const float PLASMA_BINDING_ENERGY = 20000000f;
		public const float TOROID_VOLUME_BREAKEVEN = 1000f;
		public const float FUSION_TEMPERATURE_THRESHOLD = 10000f;
		public const float PARTICLE_CHANCE_CONSTANT = -20000000f;
		public const float FUSION_RAD_MAX = 2000f;
		public const float FUSION_RAD_COEFFICIENT = -1000f;
		public const float FUSION_INSTABILITY_ENDOTHERMALITY = 2f;
		public const float FUSION_MAXIMUM_TEMPERATURE = 1e8f;

		//Other

		public const float MIASMA_CORPSE_MOLES = 0.02f;
	}
}