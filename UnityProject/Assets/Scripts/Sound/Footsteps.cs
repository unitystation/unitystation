using System.Collections.Generic;
using UnityEngine;

namespace Sound
{
	/// <summary>
	/// This class handles all the logic to play the
	/// proper footsteps of players when walking
	/// </summary>
	public class Footsteps : MonoBehaviour
	{
		private static readonly System.Random Rnd = new System.Random();
		private static bool step;

		public static void FootstepAtPosition(Vector3 worldPos, StepType stepType, GameObject performer)
		{
			MatrixInfo matrix = MatrixManager.AtPoint(worldPos.NormalizeToInt(), false);
			var locPos = matrix.ObjectParent.transform.InverseTransformPoint(worldPos).RoundToInt();
			var tile = matrix.MetaTileMap.GetTile(locPos) as BasicTile;
			
			if (tile != null)
			{
				if (step)
				{
					SoundManager.PlayNetworkedAtPos(
						stepSounds[stepType][tile.floorTileType][Rnd.Next(stepSounds[stepType][tile.floorTileType].Count)],
						worldPos,
						(float)SoundManager.Instance.GetRandomNumber(0.7d, 1.2d),
						Global: false, polyphonic: true, sourceObj: performer
					);
				}

				step = !step;
			}
		}

		/// <summary>
		/// this enum describes the type of step a mob/player should have
		/// </summary>
		public enum StepType
		{
			None,
			Barefoot,
			Claw,
			Shoes,
			Suit,
			Heavy,
			Clown
		}

		private static readonly Dictionary<StepType, Dictionary<FloorTileType, List<string>>> stepSounds = new Dictionary<StepType, Dictionary<FloorTileType, List<string>>>()
		{
			{
				StepType.Barefoot,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.carpet,
						new List<string>
							{"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4", "carpetbarefoot5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string> {"hardbarefoot1", "hardbarefoot2", "hardbarefoot3", "hardbarefoot4", "hardbarefoot5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"woodbarefoot1", "woodbarefoot2", "woodbarefoot3", "woodbarefoot4", "woodbarefoot5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					}
				}
			},
			{
				StepType.Claw,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"carpetbarefoot1", "carpetbarefoot2", "carpetbarefoot3", "carpetbarefoot4", "carpetbarefoot5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string> {"hardclaw1", "hardclaw2", "hardclaw3", "hardclaw4", "hardclaw5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"woodclaw1", "woodclaw2", "woodclaw3", "woodclaw4", "woodclaw5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					}
				}
			},
			{
				StepType.Shoes,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"floor1", "floor2", "floor3", "floor4", "floor5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"carpet1", "carpet2", "carpet3", "carpet4", "carpet5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"catwalk1", "catwalk2", "catwalk3", "catwalk4", "catwalk5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"grass1", "grass2", "grass3", "grass4"}
					},
					{
						FloorTileType.lava,
						 new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.plating,
						new List<string> {"plating1", "plating2", "plating3", "plating4", "plating5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"wood1", "wood2", "wood3", "wood4", "wood5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"asteroid1", "asteroid2", "asteroid3", "asteroid4", "asteroid5"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Suit,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.grass,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.lava,
						 new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.plating,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.wood,
						new List<string> {"suitstep1", "suitstep2", "suitstep3", "suitstep4", "suitstep5"}
					},
					{
						FloorTileType.sand,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Heavy,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.grass,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.lava,
						 new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.plating,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.wood,
						new List<string> {"heavystep1", "heavystep2"}
					},
					{
						FloorTileType.sand,
						new List<string> {"lava1", "lava2", "lava3"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					},
					{
						FloorTileType.bananium,
						new List<string> {"clownstep1", "clownstep2"}
					},
				}
			},
			{
				StepType.Clown,
				new Dictionary<FloorTileType, List<string>>
				{
					{
						FloorTileType.floor,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.asteroid,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.carpet,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.catwalk,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.grass,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.lava,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.plating,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.wood,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.sand,
						new List<string> {"clownstep1", "clownstep2"}
					},
					{
						FloorTileType.water,
						new List<string> {"water1", "water2", "water3", "water4"}
					}
				}
			}
		};
	}
}
