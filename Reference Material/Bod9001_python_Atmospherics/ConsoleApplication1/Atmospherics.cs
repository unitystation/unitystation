using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Atmospherics
{
    public class Gas
    {
        public readonly string Name;
        public readonly float HeatCapacity;
        public readonly float MolarMass;

        Gas(string name, float heatCapacity, float molarMass)
        {
            this.Name = name;
            this.HeatCapacity = heatCapacity;
            this.MolarMass = molarMass;
        }

        public static Gas Oxygen = new Gas("Oxygen", 0.659f, 31.9988f);
        public static Gas Nitrogen = new Gas("Nitrogen", 0.743f, 28.0134f);
        public static Gas Plasma = new Gas("Plasma", 0.8f, 40f);
        public static Gas CarbonDioxide = new Gas("Carbon Dioxide", 0.655f, 44.01f);
    }

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
    public struct AirTile
    {
        public const float GasConstant = 8.3144598f;
        public float Temperature;
        public float Moles;
        public bool Obstructed;
        public bool Space;

        public AirTile(float temperature, float moles, bool obstructed, bool space)
        {
            this.Temperature = temperature;
            this.Moles = moles;
            this.Obstructed = obstructed;
            this.Space = space;
        }
        public float Pressure
        {
            get
            {
                return Moles * GasConstant * Temperature / 2 / 1000;
            }
        }

        //public static readonly AirTile Obstructed = new AirTile(293.15f, 83.142422004453842459076923779184f);
        //public static readonly AirTile Space = new AirTile(2.7f, 0.000000316f);
    }

    //	public enum Air
    //	{
    //		Temperature,
    //		Pressure,
    //		Moles
    //	}

    //	public enum Matrix
    //	{
    //		Obstructed,
    //		Space
    //	}

    public static class Globals
    {
        public const int MaxWidthX = 361;
        public const int MaxWidthY = 211;
        public static Dictionary<Tuple<int, int>, Dictionary<Gas, float>> AirMixes = new Dictionary<Tuple<int, int>, Dictionary<Gas, float>>();

        public static AirTile[,] TileData = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];

        public static AirTile SpaceAir = new AirTile(2.7f, 0.000000316f, false, true);
        public static Dictionary<Gas, float> SpaceMix = new Dictionary<Gas, float>();

        public static float GasConstant = 8.3144598f;

        public static Dictionary<Tuple<int, int>, List<List<int>>> DictionaryOfAdjacents = new Dictionary<Tuple<int, int>, List<List<int>>>();

        public static HashSet<Tuple<int, int>> OddSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> EvenSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> TilesWithPlasmaSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> UpdateTileSet = new HashSet<Tuple<int, int>>();
        public static List<List<int>> EdgeTiles = new List<List<int>>();


        public static Dictionary<Tuple<int, int>, int> CheckCountDictionary = new Dictionary<Tuple<int, int>, int>();
        public static Dictionary<Tuple<int, int>, int> CheckCountDictionaryMoving = new Dictionary<Tuple<int, int>, int>();


        public static bool Lag;
        public static bool OddEven;
    }

    public static class Initialization
    {
        public static void AtmosphericsInitialization()
        {
            BoolInitialization();
            AirInitialization();
            JsonImportInitialization();
            WorseCaseUpdateSet();
            PitchPatch();
            MakingDictionaryOfAdjacents();
            MakingCheckCountDictionarys();
            SpaceInitialization();
        }

        static void BoolInitialization()
        {
            Globals.OddEven = false;
            Globals.Lag = false;
        }


        static void AirInitialization()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            Tuple<int, int> CoordinatesTuple = Tuple.Create(Globals.MaxWidthX, Globals.MaxWidthY);
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    tiles[i, j] = new AirTile(293.15f, 83.142422004453842459076923779184f, false, true);
                    Tuple<int, int> CoordinatesNowTuple = Tuple.Create(i, j);
                    var ToApplyToMixes = new Dictionary<Gas, float>
                    {
                        {Gas.Oxygen, 16.628484400890768491815384755837f}, {Gas.Nitrogen, 66.513937603563073967261539023347f}
                    };
                    Globals.AirMixes.Add(CoordinatesNowTuple, ToApplyToMixes);
                }
            }
            Globals.TileData = tiles;
            Globals.SpaceMix.Add(Gas.Oxygen, 0.000000000000281f);
            Console.WriteLine("AirInitialization done!");
        }

        static void JsonImportInitialization()
        {
            var json = File.ReadAllText(@"BoxStationStripped.json");
            var wallsFloors = JsonConvert.DeserializeObject<Dictionary<string, List<List<int>>>>(json);
            foreach (var walls in wallsFloors["Walls"])
            {
                Globals.TileData[walls[0],walls[1]].Obstructed = true;
            }

            foreach (var Floor in wallsFloors["Floor"])
            {
                Globals.TileData[Floor[0], Floor[1]].Space = false;
            }
            Console.WriteLine("JsonImportInitialization done!");
        }

        static List<List<int>> GenerateAdjacentTiles(List<int> Tile)
        {
            List<List<int>> AdjacentTilesRelativeCoordinatesList = new List<List<int>>();

            List<int> temporaryList = new List<int> { 0, 0 };
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList);

            List<int> temporaryList1 = new List<int> { 1, 0 };
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList1);

            List<int> temporaryList2 = new List<int> { 0, 1 };
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList2);

            List<int> temporaryList3 = new List<int> { -1, 0 };
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList3);

            List<int> temporaryList4 = new List<int> { 0, -1 };
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList4);

            List<List<int>> WorkedOutList = new List<List<int>>();
            for (var i = 0; i < AdjacentTilesRelativeCoordinatesList.Count; i++)
            {
                List<int> TileOffset = AdjacentTilesRelativeCoordinatesList[i];
                int WorkedOutOffset1 = TileOffset[0] + Tile[0];
                int WorkedOutOffset2 = TileOffset[1] + Tile[1];


                if (!((WorkedOutOffset1 >= Globals.MaxWidthX || WorkedOutOffset1 < 0) ||
                        (WorkedOutOffset2 >= Globals.MaxWidthY || WorkedOutOffset2 < 0)))
                {
                    List<int> subList = new List<int>() { WorkedOutOffset1, WorkedOutOffset2 };
                    WorkedOutList.Add(subList);
                }
            }

            return WorkedOutList;
        }

        public static void AVisualCheck()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            Tuple<int, int> CoordinatesTuple = Tuple.Create(Globals.MaxWidthX, Globals.MaxWidthY);
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    if (Globals.TileData[i, j].Space == false)
                    {
                        if (Globals.TileData[i, j].Obstructed == false)
                        {
                            Console.Write(CoordinatesTuple);
                            Console.Write(Globals.TileData[i, j].Moles);
                            Console.Write(Globals.TileData[i, j].Pressure);
                        }
                    }
                }
            }
        }

        static void MakingDictionaryOfAdjacents()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    List<int> CoordinatesList = new List<int> {i, j};
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(i, j);
                    List<List<int>> Adjacents = GenerateAdjacentTiles(CoordinatesList);
                    Globals.DictionaryOfAdjacents.Add(CoordinatesTuple, Adjacents);
                }
            }
            Console.WriteLine("MakingDictionaryOfAdjacents Done!");
        }


        static void WorseCaseUpdateSet()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(i, j);
                    //Console.WriteLine(CoordinatesTuple);
                    if (Globals.TileData[i, j].Obstructed == false)
                    {
                        Globals.UpdateTileSet.Add(CoordinatesTuple);
                    }
                }
            }
            Console.WriteLine("WorseCaseUpdateSet Done!");
        }


        static void PitchPatch()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(i, j);
                    if (i % 2 == 0)
                    {
                        if (j % 2 == 0)
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
                        if (j % 2 == 0)
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
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(i, j);
                    Globals.CheckCountDictionary.Add(CoordinatesTuple, 0);
                    Globals.CheckCountDictionaryMoving.Add(CoordinatesTuple, 0);
                }
            }

            Console.WriteLine("MakingCheckCountDictionarys Done!");
        }


        static void SpaceInitialization()
        {
            AirTile[,] tiles = new AirTile[Globals.MaxWidthX, Globals.MaxWidthY];
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(i, j);
                    if (Globals.TileData[i, j].Space)
                    {
                        if (Globals.TileData[i, j].Obstructed == false)
                        {
                            Globals.TileData[i, j] = Globals.SpaceAir;
                            Globals.AirMixes[CoordinatesTuple] = new Dictionary<Gas, float>(Globals.SpaceMix);
                        }
                        else
                        {
                            Globals.TileData[i, j].Space = false;
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
            List<List<int>> AdjacentTilesAndItself = Globals.DictionaryOfAdjacents[Tile];
            List<int> TileList = new List<int>() {Tile.Item1, Tile.Item2};
            var MixCalculationDictionary = new Dictionary<Gas, float>();
            var JMCalculationDictionary = new Dictionary<Gas, float>();
            bool RemoveTile = false;
            var TileWorkingOn = new List<List<int>>();
            var keyToDelete = new List<Gas>();
            bool Decay = true;
            bool IsSpace = false;
            float MolesAll = 0f;
            float Temperature = 0f;
            int Count = 0;
            //float Pressure;


            foreach (List<int> TileInWorkList in AdjacentTilesAndItself)
            {
                if (Globals.TileData[TileInWorkList[0], TileInWorkList[1]].Space)
                {
                    IsSpace = true;
                    break;
                }

                if (Globals.TileData[TileInWorkList[0], TileInWorkList[1]].Obstructed == false)
                {
                    Tuple<int, int> TileInWorkTuple = Tuple.Create(TileInWorkList[0], TileInWorkList[1]);
                    foreach (KeyValuePair<Gas, float> KeyValue in Globals.AirMixes[TileInWorkTuple])
                    {
                        if (MixCalculationDictionary.TryGetValue(KeyValue.Key, out float MixCalculationDictionarykey))
                        {
                        }
                        else
                        {
                            MixCalculationDictionarykey = 0;
                        }

                        if (JMCalculationDictionary.TryGetValue(KeyValue.Key, out float JMCalculationDictionarykey))
                        {
                        }
                        else
                        {
                            JMCalculationDictionarykey = 0;
                        }

                        MixCalculationDictionary[KeyValue.Key] = (KeyValue.Value + MixCalculationDictionarykey); //*************** 
                        JMCalculationDictionary[KeyValue.Key] =
                            ((Globals.TileData[TileInWorkList[0], TileInWorkList[1]].Temperature * KeyValue.Value) * KeyValue.Key.HeatCapacity) +
                            JMCalculationDictionarykey; //***************
                        if (KeyValue.Value < 0.000000000000001)
                        {
                            keyToDelete.Add(KeyValue.Key);
                        }
                    }

                    if (0.01f < (Math.Abs(Globals.TileData[TileList[0], TileList[1]].Pressure - Globals.TileData[TileInWorkList[0], TileInWorkList[1]].Pressure)))
                    {
                        Decay = false;
                    }

                    Count += 1;
                    TileWorkingOn.Add(TileInWorkList);
                }
            }

            if (IsSpace == false)
            {
                foreach (KeyValuePair<Gas, float> KeyValue in MixCalculationDictionary)
                {
                    float KeyMixWorkedOut = (MixCalculationDictionary[KeyValue.Key] / Count);
                    MolesAll += KeyMixWorkedOut;

                    JMCalculationDictionary[KeyValue.Key] =
                        (((JMCalculationDictionary[KeyValue.Key] / KeyValue.Key.HeatCapacity) / KeyMixWorkedOut) / JMCalculationDictionary.Count);
                    Globals.AirMixes[Tile][KeyValue.Key] = KeyMixWorkedOut;
                    Temperature += (JMCalculationDictionary[KeyValue.Key] / Count);

                    if (KeyValue.Key == Gas.Plasma)
                    {
                        if (KeyValue.Value > 0.0f) // This needs tweaking to find what the minimum amount of plasma is needed For a reaction is
                        {
                            Globals.TilesWithPlasmaSet.Add(Tile);
                        }

                        //Globals.TilesWithPlasmaSet.Add(Tile)
                    }
                }

                //Pressure = (((MolesAll * Globals.GasConstant * Temperature) / 2) / 1000);

                for (var i = 0; i < keyToDelete.Count; i++)
                {
                    Gas Key = keyToDelete[i];
                    try
                    {
                        Globals.AirMixes[Tile].Remove(Key);
                    }
                    catch (KeyNotFoundException)
                    {
                    }
                }

                foreach (List<int> TileApplyingList in TileWorkingOn)
                {
                    if (Globals.TileData[TileApplyingList[0], TileApplyingList[1]].Space == false)
                    {
                        Tuple<int, int> TileApplyingTuple = Tuple.Create(TileApplyingList[0], TileApplyingList[1]);
                        Globals.AirMixes[TileApplyingTuple] = new Dictionary<Gas, float>(Globals.AirMixes[Tile]);
                        Globals.TileData[TileApplyingList[0],TileApplyingList[1]].Temperature = Temperature;
                        Globals.TileData[TileApplyingList[0],TileApplyingList[1]].Moles = MolesAll;
                        //Globals.TileData[TileApplyingList[0],TileApplyingList[1]].Pressure = Pressure;
                    }
                }
            }
            else
            {
                foreach (List<int> TileApplyingList in TileWorkingOn)
                {
                    Tuple<int, int> TileApplyingTuple = Tuple.Create(TileApplyingList[0], TileApplyingList[1]);
                    Globals.AirMixes[TileApplyingTuple] = new Dictionary<Gas, float>(Globals.SpaceMix);

                    Globals.TileData[TileApplyingList[0], TileApplyingList[1]].Temperature = 2.7f;
                    Globals.TileData[TileApplyingList[0], TileApplyingList[1]].Moles = 0.000000000000281f;
                    //Globals.TileData[TileApplyingList[0], TileApplyingList[1]].Pressure = 0.000000316f;
                }
            }

            if (Decay)
            {
                //Console.WriteLine(Tile);
                if (Globals.CheckCountDictionaryMoving[Tile] >= 3)
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
            foreach (Tuple<int, int> TileCalculating in Globals.UpdateTileSet)
            {
                if (Globals.Lag)
                {
                    bool RemoveTile;
                    if (OddEven)
                    {
                        if (Globals.OddSet.Contains(TileCalculating))
                        {
                            RemoveTile = GasMoving(TileCalculating);
                            if (RemoveTile)
                            {
                                TilesRemoveFromUpdate.Add(TileCalculating);
                            }
                        }
                    }
                    else
                    {
                        if (Globals.EvenSet.Contains(TileCalculating))
                        {
                            RemoveTile = GasMoving(TileCalculating);
                            if (RemoveTile)
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

            foreach (Tuple<int, int> TileRemoveing in TilesRemoveFromUpdate)
            {
                List<int> TileRemoveingList = new List<int> { TileRemoveing.Item1, TileRemoveing.Item2 };
                Globals.EdgeTiles.Add(TileRemoveingList);
                Globals.UpdateTileSet.Remove(TileRemoveing);
            }

            OddEven = !OddEven;

            return OddEven;
        }


        static void DoTheEdge()
        {
            if (!Globals.EdgeTiles.Any())
            {
                return;
            }

            List<List<int>> NewEdgeTiles = new List<List<int>>();
            int CountForUpdateSet = new int();
            for (var i = 0; i < Globals.EdgeTiles.Count; i++)
            {
                List<int> TileCheckingList = Globals.EdgeTiles[i];
                Tuple<int, int> TileChecking = Tuple.Create(TileCheckingList[0], TileCheckingList[1]);
                //Console.WriteLine(TileChecking);
                List<List<int>> AdjacentTilesAndItself = new List<List<int>>(Globals.DictionaryOfAdjacents[TileChecking]);
                if (!AdjacentTilesAndItself.Any())
                {
                    continue;
                }

                AdjacentTilesAndItself.RemoveAt(0);
                var Decay = true;
                CountForUpdateSet = 0;
                for (var j = 0; j < AdjacentTilesAndItself.Count; j++)
                {
                    List<int> AdjacentTileList = AdjacentTilesAndItself[j];

                    //Console.Write(AdjacentTileTuple);
                    if (Globals.TileData[AdjacentTileList[0], AdjacentTileList[1]].Obstructed == false)
                    {
                        if (0.00001f < (Math.Abs(Globals.TileData[AdjacentTileList[0], AdjacentTileList[1]].Pressure - Math.Abs(Globals.TileData[TileCheckingList[0], TileCheckingList[1]].Pressure))))
                        {
                            Decay = false;
                            Tuple<int, int> AdjacentTileTuple = Tuple.Create(AdjacentTileList[0], AdjacentTileList[1]);
                            if (!Globals.UpdateTileSet.Contains(AdjacentTileTuple))
                            {
                                if (!NewEdgeTiles.Contains(AdjacentTileList))
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

                if (!Decay)
                {
                    continue;
                }

                if (Globals.CheckCountDictionary[TileChecking] >= 0)
                {
                    bool Decayfnleall = new bool();
                    Decayfnleall = true;
                    foreach (List<int> AdjacentTileList in AdjacentTilesAndItself)
                    {
                        Tuple<int, int> AdjacentTileTuple = Tuple.Create(AdjacentTileList[0], AdjacentTileList[1]);
                        if (Globals.UpdateTileSet.Contains(AdjacentTileTuple))
                        {
                            Decayfnleall = false;
                        }
                    }

                    if (Decayfnleall == false)
                    {
                        if (Globals.TileData[TileCheckingList[0], TileCheckingList[1]].Obstructed == false)
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

            foreach (Tuple<int, int> TilePlasma in Globals.TilesWithPlasmaSet)
            {
                float AirMixesyPlasmakey = new float();
                if (Globals.AirMixes[TilePlasma].TryGetValue(Gas.Plasma, out AirMixesyPlasmakey))
                {
                }
                else
                {
                    AirMixesyPlasmakey = 0;
                }

                if (AirMixesyPlasmakey > 0)
                {
                    List<int> TilePlasmaList = new List<int>() { TilePlasma.Item1, TilePlasma.Item2 };
                    if (Globals.TileData[TilePlasmaList[0], TilePlasmaList[1]].Temperature > 1643.15)
                    {
                        float AirMixesyOxygenkey = new float();
                        if (Globals.AirMixes[TilePlasma].TryGetValue(Gas.Oxygen, out AirMixesyOxygenkey))
                        {
                        }
                        else
                        {
                            AirMixesyOxygenkey = 0;
                        }

                        float AirMixesyCarbonDioxidekey = new float();
                        if (Globals.AirMixes[TilePlasma].TryGetValue(Gas.CarbonDioxide, out AirMixesyCarbonDioxidekey))
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
                        if (TheOxygenMoles > 1)
                        {
                            TemperatureScale = 1;
                        }
                        else
                        {
                            TemperatureScale = (Globals.TileData[TilePlasmaList[0], TilePlasmaList[1]].Temperature - 373) / (1643.15f - 373);
                        }

                        if (TemperatureScale > 0)
                        {
                            float PlasmaBurnRate = new float();
                            float ThePlasmaMoles = Globals.AirMixes[TilePlasma][Gas.Plasma];
                            float OxygenBurnRate = (1.4f - TemperatureScale);
                            if (TheOxygenMoles > ThePlasmaMoles * 10)
                            {
                                PlasmaBurnRate = ((ThePlasmaMoles * TemperatureScale) / 4);
                            }
                            else
                            {
                                PlasmaBurnRate = (TemperatureScale * (TheOxygenMoles / 10)) / 4;
                            }

                            if (PlasmaBurnRate > 0.03f)
                            {
                                float EnergyReleased = new float();
                                float FuelBurnt = new float();
                                float JM = new float();
                                float J = new float();

                                ThePlasmaMoles -= PlasmaBurnRate;
                                TheOxygenMoles -= (PlasmaBurnRate * OxygenBurnRate);
                                TheCarbonDioxideMoles += PlasmaBurnRate;

                                EnergyReleased = (3000000 * PlasmaBurnRate);
                                FuelBurnt = (PlasmaBurnRate) * (1 + OxygenBurnRate);

                                Globals.AirMixes[TilePlasma][Gas.Oxygen] = TheOxygenMoles;
                                Globals.AirMixes[TilePlasma][Gas.Plasma] = ThePlasmaMoles;
                                Globals.AirMixes[TilePlasma][Gas.CarbonDioxide] = TheCarbonDioxideMoles;

                                JM = ((Globals.TileData[TilePlasmaList[0], TilePlasmaList[1]].Temperature * TheCarbonDioxideMoles) * Gas.CarbonDioxide.HeatCapacity);
                                J = (Gas.CarbonDioxide.MolarMass * JM);
                                J += EnergyReleased;
                                JM = (J / Gas.CarbonDioxide.MolarMass);
                                Globals.TileData[TilePlasmaList[0], TilePlasmaList[1]].Temperature = ((JM / Gas.CarbonDioxide.HeatCapacity) / TheCarbonDioxideMoles);
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
            while (count++ < 100)
            {
                Stopwatch swTick = new Stopwatch();
                swTick.Start();
                Globals.Lag = true;
                OddEven = LagOvereLay(OddEven);
                AirReactions();
                DoTheEdge();
                swTick.Stop();
                float timeTaken = 0.001f * swTick.ElapsedMilliseconds;
                if (timeTaken > 0.2f)
                {
                    if (timeTaken < 0.1f)
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
            Console.WriteLine("Count:{0} Number of passers", count);
            Console.WriteLine("Count:{0} UpdateTileSet", Globals.UpdateTileSet.Count);
            Console.WriteLine("Count:{0} EdgeTiles", Globals.EdgeTiles.Count);
        }
    }
}