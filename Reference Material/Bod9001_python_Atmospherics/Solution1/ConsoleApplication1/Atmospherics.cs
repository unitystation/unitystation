using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Atmospherics
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			Console.WriteLine("Press any key to continue . . . ");
			Console.ReadKey();
			Initialization.AtmosphericsInitialization();
			AtmosphericsTime.Atmospherics();
//			Initialization.AVisualCheck();
			Console.WriteLine("Press any key to continue . . . ");
			Console.ReadKey();
		}
	}

	public enum Gas
	{
		Oxygen,
		Nitrogen,
		Plasma,
		CarbonDioxide
	}
	public enum Air
	{
		Temperature,
		Pressure,
		Moles
	}

	public enum Matrix
	{
		Obstructed,
		Space
	}

	public static class Globals
	{
		public static List<int> TileRange = new List<int>();
		public static Dictionary<Tuple<int, int>, Dictionary<Air, float>> Air = new Dictionary<Tuple<int, int>, Dictionary<Air, float>>();
		public static Dictionary<Tuple<int, int>, Dictionary<Gas, float>> AirMixes = new Dictionary<Tuple<int, int>, Dictionary<Gas, float>>();
		public static Dictionary<Tuple<int, int>, Dictionary<Matrix, bool>> Airbools = new Dictionary<Tuple<int, int>, Dictionary<Matrix, bool>>();

		public static Dictionary<Air, float> SpaceAir = new Dictionary<Air, float>();
		public static Dictionary<Gas, float> SpaceMix = new Dictionary<Gas, float>();

		public static float GasConstant;

		public static Dictionary<Tuple<int, int>, List<Tuple<int, int>>> DictionaryOfAdjacents = new Dictionary<Tuple<int, int>, List<Tuple<int, int>>>();

		public static HashSet<Tuple<int, int>> OddSet = new HashSet<Tuple<int, int>>();
		public static HashSet<Tuple<int, int>> EvenSet = new HashSet<Tuple<int, int>>();
		public static HashSet<Tuple<int, int>> TilesWithPlasmaSet = new HashSet<Tuple<int, int>>();
		public static HashSet<Tuple<int, int>> UpdateTileSet = new HashSet<Tuple<int, int>>();
		public static List<List<int>> EdgeTiles = new List<List<int>>();


		public static Dictionary<Tuple<int, int>, int> CheckCountDictionary = new Dictionary<Tuple<int, int>, int>();
		public static Dictionary<Tuple<int, int>, int> CheckCountDictionaryMoving = new Dictionary<Tuple<int, int>, int>();

		public static Dictionary<Gas, float> HeatCapacityOfGases = new Dictionary<Gas, float>();
		public static Dictionary<Gas, float> MolarMassesOfGases = new Dictionary<Gas, float>();

		public static bool Lag;
		public static bool OddEven;
	}

	public static class Initialization
	{
		public static void AtmosphericsInitialization()
		{
			HeatCapacityInitialization();
			AirInitialization();
			JsonImportInitialization();
			WorseCaseUpdateSet();
			PitchPatch();
			MakingDictionaryOfAdjacents();
			MakingCheckCountDictionarys();
			SpaceInitialization();
		}

		static void HeatCapacityInitialization()
		{
			Globals.TileRange.Add(361);
			Globals.TileRange.Add(211);

			Globals.OddEven = false;
			Globals.Lag = false;
			Globals.HeatCapacityOfGases.Add(Gas.Oxygen, 0.659f);
			Globals.HeatCapacityOfGases.Add(Gas.Nitrogen, 0.743f);
			Globals.HeatCapacityOfGases.Add(Gas.Plasma, 0.8f);
			Globals.HeatCapacityOfGases.Add(Gas.CarbonDioxide, 0.655f);

			Globals.MolarMassesOfGases.Add(Gas.Oxygen, 31.9988f);
			Globals.MolarMassesOfGases.Add(Gas.Nitrogen, 28.0134f);
			Globals.MolarMassesOfGases.Add(Gas.Plasma, 40f);
			Globals.MolarMassesOfGases.Add(Gas.CarbonDioxide, 44.01f);


			Globals.GasConstant = 8.3144598f;
		}


		static void AirInitialization()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				foreach ( int Number2 in numberList2 )
				{
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					var ToApplyAir = new Dictionary<Air, float>
					{
						{Air.Temperature, 293.15f}, {Air.Pressure, 101.325f}, {Air.Moles, 83.142422004453842459076923779184f}
					};
					Globals.Air.Add(CoordinatesTuple, ToApplyAir);


					var ToApplyToMixes = new Dictionary<Gas, float>
					{
						{Gas.Oxygen, 16.628484400890768491815384755837f}, {Gas.Nitrogen, 66.513937603563073967261539023347f}
					};
					Globals.AirMixes.Add(CoordinatesTuple, ToApplyToMixes);

					var ToApplyToStructuralbools = new Dictionary<Matrix, bool>();
					ToApplyToStructuralbools[Matrix.Obstructed] = false;
					ToApplyToStructuralbools[Matrix.Space] = true;
					var copy = new Dictionary<Matrix, bool>(ToApplyToStructuralbools);
					Globals.Airbools[CoordinatesTuple] = copy;

					//Console.WriteLine(Number1 + "yes" + Number2);
					//Console.WriteLine(Globals.Airbools[]);
				}
			}

			Globals.SpaceAir.Add(Air.Temperature, 2.7f);
			Globals.SpaceAir.Add(Air.Pressure, 0.000000316f);
			Globals.SpaceAir.Add(Air.Moles, 0.000000000000281f);
			Globals.SpaceMix.Add(Gas.Oxygen, 0.000000000000281f);
			Console.WriteLine("AirInitialization done!");
		}

		static void JsonImportInitialization()
		{
			var json = File.ReadAllText(@"BoxStationStripped.json");
			var wallsFloors = JsonConvert.DeserializeObject<Dictionary<string, List<List<int>>>>(json);
			foreach ( var walls in wallsFloors["Walls"] )
			{
				Tuple<int, int> wallsT = Tuple.Create(walls[0], walls[1]);
				//Console.WriteLine(wallsT);
				Globals.Airbools[wallsT][Matrix.Obstructed] = true;
			}

			foreach ( var Floor in wallsFloors["Floor"] )
			{
				Tuple<int, int> FloorT = Tuple.Create(Floor[0], Floor[1]);
				Globals.Airbools[FloorT][Matrix.Space] = false;
			}

			Console.WriteLine("JsonImportInitialization done!");
		}

		static List<Tuple<int, int>> GenerateAdjacentTiles(List<int> Tile)
		{
			List<List<int>> AdjacentTilesRelativeCoordinatesList = new List<List<int>>();

			List<int> temporaryList = new List<int> {0, 0};
			AdjacentTilesRelativeCoordinatesList.Add(temporaryList);

			List<int> temporaryList1 = new List<int> {1, 0};
			AdjacentTilesRelativeCoordinatesList.Add(temporaryList1);

			List<int> temporaryList2 = new List<int> {0, 1};
			AdjacentTilesRelativeCoordinatesList.Add(temporaryList2);

			List<int> temporaryList3 = new List<int> {-1, 0};
			AdjacentTilesRelativeCoordinatesList.Add(temporaryList3);

			List<int> temporaryList4 = new List<int> {0, -1};
			AdjacentTilesRelativeCoordinatesList.Add(temporaryList4);

			List<Tuple<int, int>> WorkedOutList = new List<Tuple<int, int>>();
			for ( var i = 0; i < AdjacentTilesRelativeCoordinatesList.Count; i++ )
			{
				List<int> TileOffset = AdjacentTilesRelativeCoordinatesList[i];
				int WorkedOutOffset1 = TileOffset[0] + Tile[0];
				int WorkedOutOffset2 = TileOffset[1] + Tile[1];


				if ( !( ( WorkedOutOffset1 >= Globals.TileRange[0] || WorkedOutOffset1 < 0 ) ||
				        ( WorkedOutOffset2 >= Globals.TileRange[1] || WorkedOutOffset2 < 0 ) ) )
				{
					Tuple<int, int> subList = Tuple.Create(WorkedOutOffset1, WorkedOutOffset2);
					WorkedOutList.Add(subList);
				}
			}

			//foreach (var sublist in WorkedOutList)
			//{
			//	foreach (var obj in sublist)
			//	{
			//		Console.WriteLine(obj);
			//	}
			//}
			return WorkedOutList;
		}

		public static void AVisualCheck()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			foreach ( int Number1 in NumberList1 )
			{
				//Console.WriteLine(Number1);
				foreach ( int Number2 in NumberList2 )
				{
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					//Console.WriteLine(Number2);
					if ( Globals.Airbools[CoordinatesTuple][Matrix.Space] == false )
					{
						if ( Globals.Airbools[CoordinatesTuple][Matrix.Obstructed] == false )
						{
							Console.Write(CoordinatesTuple);
							Console.Write(Globals.Air[CoordinatesTuple][Air.Pressure]);
						}
					}
				}
			}
		}

		static void MakingDictionaryOfAdjacents()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				foreach ( int Number2 in numberList2 )
				{
					List<int> CoordinatesList = new List<int> {Number1, Number2};
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					List<Tuple<int, int>> Adjacents = GenerateAdjacentTiles(CoordinatesList);
					Globals.DictionaryOfAdjacents.Add(CoordinatesTuple, Adjacents);
				}
			}


			Console.WriteLine("MakingDictionaryOfAdjacents Done!");
		}


		static void WorseCaseUpdateSet()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				//Console.WriteLine(Number1);
				foreach ( int Number2 in numberList2 )
				{
					//Console.WriteLine(Number2);
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					//Console.WriteLine(CoordinatesTuple);
					if ( Globals.Airbools[CoordinatesTuple][Matrix.Obstructed] == false )
					{
						Globals.UpdateTileSet.Add(CoordinatesTuple);
					}
				}
			}

			Console.WriteLine("WorseCaseUpdateSet Done!");
		}


		static void PitchPatch()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				foreach ( int Number2 in numberList2 )
				{
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					if ( Number1 % 2 == 0 )
					{
						if ( Number2 % 2 == 0 )
						{
							Globals.OddSet.Add(CoordinatesTuple);
						}
						else
						{
							Globals.EvenSet.Add(CoordinatesTuple);
						}
					}
					else
					{
						if ( Number2 % 2 == 0 )
						{
							Globals.EvenSet.Add(CoordinatesTuple);
						}
						else
						{
							Globals.OddSet.Add(CoordinatesTuple);
						}
					}
				}
			}

			Console.WriteLine("PitchPatch Done!");
		}


		static void MakingCheckCountDictionarys()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				foreach ( int Number2 in numberList2 )
				{
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					//Console.WriteLine(CoordinatesTuple);
					Globals.CheckCountDictionary.Add(CoordinatesTuple, 0);
					Globals.CheckCountDictionaryMoving.Add(CoordinatesTuple, 0);
				}
			}

			Console.WriteLine("MakingCheckCountDictionarys Done!");
		}


		static void SpaceInitialization()
		{
			IEnumerable<int> NumberList1 = Enumerable.Range(0, ( Globals.TileRange[0] ));
			IEnumerable<int> NumberList2 = Enumerable.Range(0, ( Globals.TileRange[1] ));
			IEnumerable<int> numberList2 = NumberList2 as int[] ?? NumberList2.ToArray();
			foreach ( int Number1 in NumberList1 )
			{
				foreach ( int Number2 in numberList2 )
				{
					Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
					if ( Globals.Airbools[CoordinatesTuple][Matrix.Space] )
					{
						if ( Globals.Airbools[CoordinatesTuple][Matrix.Obstructed] == false )
						{
							Globals.Air[CoordinatesTuple] = new Dictionary<Air, float>(Globals.SpaceAir);
							Globals.AirMixes[CoordinatesTuple] = new Dictionary<Gas, float>(Globals.SpaceMix);
						}
						else
						{
							Globals.Airbools[CoordinatesTuple][Matrix.Space] = false;
						}
					}
				}
			}
		}
	}

	public static class AtmosphericsTime
	{
		static bool GasMoving(Tuple<int, int> Tile)
		{
			List<Tuple<int, int>> AdjacentTilesAndItself = Globals.DictionaryOfAdjacents[Tile];
			var MixCalculationDictionary = new Dictionary<Gas, float>();
			var JMCalculationDictionary = new Dictionary<Gas, float>();
			bool RemoveTile = false;
			var TileWorkingOn = new HashSet<Tuple<int, int>>();
			var keyToDelete = new List<Gas>();
			bool Decay = true;
			bool IsSpace = false;
			float MolesAll = 0f;
			float Temperature = 0f;
			int Count = 0;
			float Pressure;


			foreach ( Tuple<int, int> TileInWorkList in AdjacentTilesAndItself )
			{
				if ( Globals.Airbools[TileInWorkList][Matrix.Space] )
				{
					IsSpace = true;
					break;
				}

				if ( Globals.Airbools[TileInWorkList][Matrix.Obstructed] == false )
				{
					foreach ( KeyValuePair<Gas, float> KeyValue in Globals.AirMixes[TileInWorkList] )
					{
						float MixCalculationDictionarykey;
						if ( MixCalculationDictionary.TryGetValue(KeyValue.Key, out MixCalculationDictionarykey) )
						{
						}
						else
						{
							MixCalculationDictionarykey = 0;
						}

						float JMCalculationDictionarykey;
						if ( JMCalculationDictionary.TryGetValue(KeyValue.Key, out JMCalculationDictionarykey) )
						{
						}
						else
						{
							JMCalculationDictionarykey = 0;
						}

						MixCalculationDictionary[KeyValue.Key] = ( KeyValue.Value + MixCalculationDictionarykey ); //*************** 
						JMCalculationDictionary[KeyValue.Key] =
							( ( Globals.Air[TileInWorkList][Air.Temperature] * KeyValue.Value ) * Globals.HeatCapacityOfGases[KeyValue.Key] ) +
							JMCalculationDictionarykey; //***************
						if ( KeyValue.Value < 0.000000000000001 )
						{
							keyToDelete.Add(KeyValue.Key);
						}
					}

					if ( 0.01f < ( Math.Abs(Globals.Air[Tile][Air.Pressure] - Globals.Air[TileInWorkList][Air.Pressure]) ) )
					{
						Decay = false;
					}

					Count += 1;
					TileWorkingOn.Add(TileInWorkList);
				}
			}

			if ( IsSpace == false )
			{
				foreach ( KeyValuePair<Gas, float> KeyValue in MixCalculationDictionary )
				{
					float KeyMixWorkedOut = ( MixCalculationDictionary[KeyValue.Key] / Count );
					MolesAll += KeyMixWorkedOut;

					JMCalculationDictionary[KeyValue.Key] =
						( ( ( JMCalculationDictionary[KeyValue.Key] / Globals.HeatCapacityOfGases[KeyValue.Key] ) / KeyMixWorkedOut ) / JMCalculationDictionary.Count );
					Globals.AirMixes[Tile][KeyValue.Key] = KeyMixWorkedOut;
					Temperature += ( JMCalculationDictionary[KeyValue.Key] / Count );

					if ( KeyValue.Key == Gas.Plasma )
					{
						if ( KeyValue.Value > 0.0f ) // This needs tweaking to find what the minimum amount of plasma is needed For a reaction is
						{
							Globals.TilesWithPlasmaSet.Add(Tile);
						}

						//Globals.TilesWithPlasmaSet.Add(Tile)
					}
				}

				Pressure = ( ( ( MolesAll * Globals.GasConstant * Temperature ) / 2 ) / 1000 );

				for ( var i = 0; i < keyToDelete.Count; i++ )
				{
					Gas Key = keyToDelete[i];
					try
					{
						Globals.AirMixes[Tile].Remove(Key);
					}
					catch ( KeyNotFoundException )
					{
					}
				}

				foreach ( Tuple<int, int> TileApplyingList in TileWorkingOn )
				{
					if ( Globals.Airbools[TileApplyingList][Matrix.Space] == false )
					{
						Globals.AirMixes[TileApplyingList] = new Dictionary<Gas, float>(Globals.AirMixes[Tile]);
						Globals.Air[TileApplyingList][Air.Temperature] = Temperature;
						Globals.Air[TileApplyingList][Air.Moles] = MolesAll;
						Globals.Air[TileApplyingList][Air.Pressure] = Pressure;
					}
				}
			}
			else
			{
				foreach ( Tuple<int, int> TileApplyingList in TileWorkingOn )
				{
					Globals.AirMixes[TileApplyingList] = new Dictionary<Gas, float>(Globals.SpaceMix);

					Globals.Air[TileApplyingList][Air.Temperature] = 2.7f;
					Globals.Air[TileApplyingList][Air.Moles] = 0.000000000000281f;
					Globals.Air[TileApplyingList][Air.Pressure] = 0.000000316f;
				}
			}

			if ( Decay )
			{
				//Console.WriteLine(Tile);
				if ( Globals.CheckCountDictionaryMoving[Tile] >= 3 )
				{
					RemoveTile = true;
					Globals.CheckCountDictionaryMoving[Tile] = 0;
				}
				else
				{
					Globals.CheckCountDictionaryMoving[Tile] += 1;
				}
			}

			return RemoveTile;
		}


		static bool LagOvereLay(bool OddEven)
		{
			HashSet<Tuple<int, int>> TilesRemoveFromUpdate = new HashSet<Tuple<int, int>>();
			foreach ( Tuple<int, int> TileCalculating in Globals.UpdateTileSet )
			{
				if ( Globals.Lag )
				{
					bool RemoveTile;
					if ( OddEven )
					{
						if ( Globals.OddSet.Contains(TileCalculating) )
						{
							RemoveTile = GasMoving(TileCalculating);
							if ( RemoveTile )
							{
								TilesRemoveFromUpdate.Add(TileCalculating);
							}
						}
					}
					else
					{
						if ( Globals.EvenSet.Contains(TileCalculating) )
						{
							RemoveTile = GasMoving(TileCalculating);
							if ( RemoveTile )
							{
								TilesRemoveFromUpdate.Add(TileCalculating);
							}
						}
					}
				}
//				else
//				{
//					RemoveTile = GasMoving(TileCalculating);
//					if ( RemoveTile )
//					{
//						TilesRemoveFromUpdate.Add(TileCalculating);
//					}
//				}
			}

			foreach ( Tuple<int, int> TileRemoveing in TilesRemoveFromUpdate )
			{
				List<int> TileRemoveingList = new List<int> {TileRemoveing.Item1, TileRemoveing.Item2};
				Globals.EdgeTiles.Add(TileRemoveingList);
				Globals.UpdateTileSet.Remove(TileRemoveing);
			}

			OddEven = !OddEven;

			return OddEven;
		}


		static void DoTheEdge()
		{
			if ( !Globals.EdgeTiles.Any() )
			{
				return;
			}

			List<List<int>> NewEdgeTiles = new List<List<int>>();
			int CountForUpdateSet = new int();
			for ( var i = 0; i < Globals.EdgeTiles.Count; i++ )
			{
				List<int> TileCheckingList = Globals.EdgeTiles[i];
				Tuple<int, int> TileChecking = Tuple.Create(TileCheckingList[0], TileCheckingList[1]);
				//Console.WriteLine(TileChecking);
				List<Tuple<int, int>> AdjacentTilesAndItself = new List<Tuple<int, int>>(Globals.DictionaryOfAdjacents[TileChecking]);
				if ( !AdjacentTilesAndItself.Any() )
				{
					continue;
				}

				AdjacentTilesAndItself.RemoveAt(0);
				var Decay = true;
				CountForUpdateSet = 0;
				for ( var j = 0; j < AdjacentTilesAndItself.Count; j++ )
				{
					Tuple<int, int> AdjacentTileTuple = AdjacentTilesAndItself[j];
//Console.Write(AdjacentTileTuple);
					if ( Globals.Airbools[AdjacentTileTuple][Matrix.Obstructed] == false )
					{
						if ( 0.00001f < ( Math.Abs(Globals.Air[AdjacentTileTuple][Air.Pressure] - Globals.Air[TileChecking][Air.Pressure]) ) )
						{
							Decay = false;
							if ( !Globals.UpdateTileSet.Contains(AdjacentTileTuple) )
							{
								List<int> AdjacentTileList = new List<int> {AdjacentTileTuple.Item1, AdjacentTileTuple.Item2};
								if ( !NewEdgeTiles.Contains(AdjacentTileList) )
								{
									Globals.UpdateTileSet.Add(AdjacentTileTuple);
									NewEdgeTiles.Add(AdjacentTileList);
								}
								else
								{
									Globals.UpdateTileSet.Add(AdjacentTileTuple);
								}
							}
							else
							{
								CountForUpdateSet += 1;
							}
						}
					}
				}

				if ( !Decay )
				{
					continue;
				}

				if ( Globals.CheckCountDictionary[TileChecking] >= 0 )
				{
					bool Decayfnleall = new bool();
					Decayfnleall = true;
					foreach ( Tuple<int, int> AdjacentTileTuple in AdjacentTilesAndItself )
					{
						if ( Globals.UpdateTileSet.Contains(AdjacentTileTuple) )
						{
							Decayfnleall = false;
						}
					}

					if ( Decayfnleall == false )
					{
						if ( Globals.Airbools[TileChecking][Matrix.Obstructed] == false )
						{
							NewEdgeTiles.Add(TileCheckingList);
						}
					}

					Globals.CheckCountDictionary[TileChecking] = 0;
				}
				else
				{
					Globals.CheckCountDictionary[TileChecking] += 1;
					//NewEdgeTiles.Add(TileCheckingList);
				}

				//else
				//{
				//    if (CountForUpdateSet > 1)
				//    {
				//        Globals.UpdateTileSet.Add(AdjacentTileTuple);
				//    }
				//{
			}

			Globals.EdgeTiles = new List<List<int>>(NewEdgeTiles);
		}

		static void AirReactions()
		{
			var TilesWithPlasmaSetCopy = new HashSet<Tuple<int, int>>(Globals.TilesWithPlasmaSet);

			foreach ( Tuple<int, int> TilePlasma in Globals.TilesWithPlasmaSet )
			{
				float AirMixesyPlasmakey = new float();
				if ( Globals.AirMixes[TilePlasma].TryGetValue(Gas.Plasma, out AirMixesyPlasmakey) )
				{
				}
				else
				{
					AirMixesyPlasmakey = 0;
				}

				if ( AirMixesyPlasmakey > 0 )
				{
					if ( Globals.Air[TilePlasma][Air.Temperature] > 1643.15 )
					{
						float AirMixesyOxygenkey = new float();
						if ( Globals.AirMixes[TilePlasma].TryGetValue(Gas.Oxygen, out AirMixesyOxygenkey) )
						{
						}
						else
						{
							AirMixesyOxygenkey = 0;
						}

						float AirMixesyCarbonDioxidekey = new float();
						if ( Globals.AirMixes[TilePlasma].TryGetValue(Gas.CarbonDioxide, out AirMixesyCarbonDioxidekey) )
						{
						}
						else
						{
							AirMixesyCarbonDioxidekey = 0;
						}

						float TemperatureScale = new float();
						float TheOxygenMoles = AirMixesyOxygenkey;
						float TheCarbonDioxideMoles = new float();
						TheCarbonDioxideMoles = AirMixesyCarbonDioxidekey;
						if ( TheOxygenMoles > 1 )
						{
							TemperatureScale = 1;
						}
						else
						{
							TemperatureScale = ( Globals.Air[TilePlasma][Air.Temperature] - 373 ) / ( 1643.15f - 373 );
						}

						if ( TemperatureScale > 0 )
						{
							float PlasmaBurnRate = new float();
							float ThePlasmaMoles = Globals.AirMixes[TilePlasma][Gas.Plasma];
							float OxygenBurnRate = ( 1.4f - TemperatureScale );
							if ( TheOxygenMoles > ThePlasmaMoles * 10 )
							{
								PlasmaBurnRate = ( ( ThePlasmaMoles * TemperatureScale ) / 4 );
							}
							else
							{
								PlasmaBurnRate = ( TemperatureScale * ( TheOxygenMoles / 10 ) ) / 4;
							}

							if ( PlasmaBurnRate > 0.03f )
							{
								float EnergyReleased = new float();
								float FuelBurnt = new float();
								float JM = new float();
								float J = new float();

								ThePlasmaMoles -= PlasmaBurnRate;
								TheOxygenMoles -= ( PlasmaBurnRate * OxygenBurnRate );
								TheCarbonDioxideMoles += PlasmaBurnRate;

								EnergyReleased = ( 3000000 * PlasmaBurnRate );
								FuelBurnt = ( PlasmaBurnRate ) * ( 1 + OxygenBurnRate );

								Globals.AirMixes[TilePlasma][Gas.Oxygen] = TheOxygenMoles;
								Globals.AirMixes[TilePlasma][Gas.Plasma] = ThePlasmaMoles;
								Globals.AirMixes[TilePlasma][Gas.CarbonDioxide] = TheCarbonDioxideMoles;

								JM = ( ( Globals.Air[TilePlasma][Air.Temperature] * TheCarbonDioxideMoles ) * Globals.HeatCapacityOfGases[Gas.CarbonDioxide] );
								J = ( Globals.MolarMassesOfGases[Gas.CarbonDioxide] * JM );
								J += EnergyReleased;
								JM = ( J / Globals.MolarMassesOfGases[Gas.CarbonDioxide] );
								Globals.Air[TilePlasma][Air.Temperature] = ( ( JM / Globals.HeatCapacityOfGases[Gas.CarbonDioxide] ) / TheCarbonDioxideMoles );
							}
						}
					}
				}
			}
		}


		public static void Atmospherics()
		{
			int count = 1;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			var OddEven = true;
			while ( count++ < 100 )
			{
				Stopwatch swTick = new Stopwatch();
				swTick.Start();
				Globals.Lag = true;
				OddEven = LagOvereLay(OddEven);
				AirReactions();
				DoTheEdge();
				swTick.Stop();
				float timeTaken = 0.001f * swTick.ElapsedMilliseconds;
				if ( timeTaken > 0.2f )
				{
					if ( timeTaken < 0.1f )
					{
						Globals.Lag = false;
					}
				}
				else
				{
					Globals.Lag = true;
				}

				Console.WriteLine(swTick.Elapsed);
			}

			sw.Stop();
			Console.WriteLine(sw.Elapsed);
			Console.WriteLine("Count:{0} ", Globals.UpdateTileSet.Count);
			Console.WriteLine("Count:{0} ", Globals.EdgeTiles.Count);
		}
	}
}