using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Atmospherics
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey();
            //Initialization.JsonImportInitialization();
            Initialization.AtmosphericsInitialization();
            AtmosphericsTime.Atmospherics();
            //Initialization.AVisualCheck();
            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey();
        }


    }
    public static class Globals
    {

        public static List<int> TileRange = new List<int>();
        public static Dictionary<Tuple<int, int>, Dictionary<string, float>> Air = new Dictionary<Tuple<int, int>, Dictionary<string, float>>();
        public static Dictionary<Tuple<int, int>, Dictionary<string, float>> AirMixes = new Dictionary<Tuple<int, int>, Dictionary<string, float>>();
        public static Dictionary<Tuple<int, int>, Dictionary<string, bool>> Airbools = new Dictionary<Tuple<int, int>, Dictionary<string, bool>>();

        public static Dictionary<string, float> SpaceAir = new Dictionary<string, float>();
        public static Dictionary<string, float> SpaceMix = new Dictionary<string, float>();

        public static float GasConstant = new float();

        public static Dictionary<Tuple<int, int>, List<Tuple<int, int>>> DictionaryOfAdjacents = new Dictionary<Tuple<int, int>, List<Tuple<int, int>>>();

        public static HashSet<Tuple<int, int>> OddSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> EvenSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> TilesWithPlasmaSet = new HashSet<Tuple<int, int>>();
        public static HashSet<Tuple<int, int>> UpdateTileSet = new HashSet<Tuple<int, int>>();
        public static List<List<int>> EdgeTiles = new List<List<int>>();


        public static Dictionary<Tuple<int, int>, int> CheckCountDictionary = new Dictionary<Tuple<int, int>, int>();
        public static Dictionary<Tuple<int, int>, int> CheckCountDictionaryMoving = new Dictionary<Tuple<int, int>, int>();

        public static Dictionary<string, float> HeatCapacityOfGases = new Dictionary<string, float>();
        public static Dictionary<string, float> MolarMassesOfGases = new Dictionary<string, float>();

        public static bool Lag = new bool();
        public static bool OddEven = new bool();


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
            Globals.HeatCapacityOfGases.Add("Oxygen", 0.659f);
            Globals.HeatCapacityOfGases.Add("Nitrogen", 0.743f);
            Globals.HeatCapacityOfGases.Add("Plasma", 0.8f);
            Globals.HeatCapacityOfGases.Add("Carbon Dioxide", 0.655f);

            Globals.MolarMassesOfGases.Add("Oxygen", 31.9988f);
            Globals.MolarMassesOfGases.Add("Nitrogen", 28.0134f);
            Globals.MolarMassesOfGases.Add("Plasma", 40f);
            Globals.MolarMassesOfGases.Add("Carbon Dioxide", 44.01f);



            Globals.GasConstant = 8.3144598f;
        }



        static void AirInitialization()
        {

            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1) 
            {
                foreach (int Number2 in NumberList2)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    Dictionary<string, float> ToApplyAir = new Dictionary<string, float>();
                    ToApplyAir.Add("Temperature", 293.15f);
                    ToApplyAir.Add("Pressure", 101.325f);
                    ToApplyAir.Add("Moles", 83.142422004453842459076923779184f);
                    Globals.Air.Add(CoordinatesTuple, ToApplyAir);


                    Dictionary<string, float> ToApplyToMixes = new Dictionary<string, float>();
                    ToApplyToMixes.Add("Oxygen", 16.628484400890768491815384755837f);
                    ToApplyToMixes.Add("Nitrogen", 66.513937603563073967261539023347f);
                    Globals.AirMixes.Add(CoordinatesTuple, ToApplyToMixes);

                    Dictionary<string, bool> ToApplyToStructuralbools = new Dictionary<string, bool>();
                    ToApplyToStructuralbools["Obstructed"] = false;
                    ToApplyToStructuralbools["Space"] = true;
                    Dictionary<string, bool> copy = new Dictionary<string, bool>(ToApplyToStructuralbools);
                    Globals.Airbools[CoordinatesTuple] = copy;

                    //Console.WriteLine(Number1 + "yes" + Number2);
                    //Console.WriteLine(Globals.Airbools[]);

                }

            }
            Globals.SpaceAir.Add("Temperature", 2.7f);
            Globals.SpaceAir.Add("Pressure", 0.000000316f);
            Globals.SpaceAir.Add("Moles", 0.000000000000281f);
            Globals.SpaceMix.Add("Oxygen", 0.000000000000281f);
            Console.WriteLine("AirInitialization done!");
        }
        static void JsonImportInitialization()
        {
            var json = System.IO.File.ReadAllText(@"BoxStationStripped.json");
            var wallsFloors = JsonConvert.DeserializeObject<Dictionary<string, List<List<int>>>>(json);
            foreach (var walls in wallsFloors["Walls"])
            {
                Tuple<int, int> wallsT = Tuple.Create(walls[0], walls[1]);
                //Console.WriteLine(wallsT);
                Globals.Airbools[wallsT]["Obstructed"] = true;
            }
            foreach (var Floor in wallsFloors["Floor"])
            {
                Tuple<int, int> FloorT = Tuple.Create(Floor[0], Floor[1]);
                Globals.Airbools[FloorT]["Space"] = false;
            }
            Console.WriteLine("JsonImportInitialization done!");
        }
        static List<Tuple<int, int>> GenerateAdjacentTiles(List<int> Tile)
        {


            List<List<int>> AdjacentTilesRelativeCoordinatesList = new List<List<int>>();

            List<int> temporaryList = new List<int>()
            {0,0};
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList);

            List<int> temporaryList1 = new List<int>()
            {1,0};
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList1);

            List<int> temporaryList2 = new List<int>()
            {0,1};
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList2);

            List<int> temporaryList3 = new List<int>()
            {-1,0};
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList3);

            List<int> temporaryList4 = new List<int>()
            {0,-1};
            AdjacentTilesRelativeCoordinatesList.Add(temporaryList4);

            List<Tuple<int, int>> WorkedOutList = new List<Tuple<int, int>>();
            foreach (List<int> TileOffset in AdjacentTilesRelativeCoordinatesList)
            {
                int WorkedOutOffset1 = TileOffset[0] + Tile[0];
                int WorkedOutOffset2 = TileOffset[1] + Tile[1];

                

                if (!((WorkedOutOffset1 >= Globals.TileRange[0]|| WorkedOutOffset1 < 0) || (WorkedOutOffset2 >= Globals.TileRange[1] || WorkedOutOffset2 < 0)))
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
            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                //Console.WriteLine(Number1);
                foreach (int Number2 in NumberList2)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    //Console.WriteLine(Number2);
                    if (Globals.Airbools[CoordinatesTuple]["Space"] == false)
                    {
                        if (Globals.Airbools[CoordinatesTuple]["Obstructed"] == false)
                        {
                            Console.Write(CoordinatesTuple);
                            Console.Write(Globals.Air[CoordinatesTuple]["Pressure"]);
                        }
                        
                    }
                    
                    
                }
            }
        }
        static void MakingDictionaryOfAdjacents()
        {

            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                foreach (int Number2 in NumberList2)
                {
                    List<int> CoordinatesList = new List<int>()
                    {Number1, Number2};
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    List<Tuple<int,int>> Adjacents = GenerateAdjacentTiles(CoordinatesList);
                    Globals.DictionaryOfAdjacents.Add(CoordinatesTuple, Adjacents);
                }
            }


            Console.WriteLine("MakingDictionaryOfAdjacents Done!");
        }



        static void WorseCaseUpdateSet()
        {
            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                //Console.WriteLine(Number1);
                foreach (int Number2 in NumberList2)
                {
                    //Console.WriteLine(Number2);
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    //Console.WriteLine(CoordinatesTuple);
                    if (Globals.Airbools[CoordinatesTuple]["Obstructed"] == false)
                    {
                        Globals.UpdateTileSet.Add(CoordinatesTuple);
                    }
                }
            }
            Console.WriteLine("WorseCaseUpdateSet Done!");
        }


        static void PitchPatch()
        {


            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                foreach (int Number2 in NumberList2)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    if (Number1 % 2 == 0)
                    {
                        if (Number2 % 2 == 0)
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
                        if (Number2 % 2 == 0)
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



            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                foreach (int Number2 in NumberList2)
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
            IEnumerable<int> NumberList1 = Enumerable.Range(0, (Globals.TileRange[0]));
            IEnumerable<int> NumberList2 = Enumerable.Range(0, (Globals.TileRange[1]));
            foreach (int Number1 in NumberList1)
            {
                foreach (int Number2 in NumberList2)
                {
                    Tuple<int, int> CoordinatesTuple = Tuple.Create(Number1, Number2);
                    if (Globals.Airbools[CoordinatesTuple]["Space"] == true)
                    {
                        if (Globals.Airbools[CoordinatesTuple]["Obstructed"] == false)
                        {
                            Globals.Air[CoordinatesTuple] = new Dictionary<string, float>(Globals.SpaceAir);
                            Globals.AirMixes[CoordinatesTuple] = new Dictionary<string, float>(Globals.SpaceMix);
                        }
                        else
                        {
                            Globals.Airbools[CoordinatesTuple]["Space"] = false;
                        }
                    }

                }

            }
        }

    }
    public static class AtmosphericsTime
    {




        static bool GasMoving(Tuple<int,int> Tile)
        {
            List<Tuple<int,int>> AdjacentTilesAndItself = Globals.DictionaryOfAdjacents[Tile];
            Dictionary<string, float> MixCalculationDictionary = new Dictionary<string, float>();
            Dictionary<string, float> JMCalculationDictionary = new Dictionary<string, float>();
            bool RemoveTile = new bool();
            RemoveTile = false;
            HashSet<Tuple<int, int>> TileWorkingOn = new HashSet<Tuple<int,int>>();
            List<string> keyToDelete = new List<string>();
            bool Decay = new bool();
            Decay = true;
            bool IsSpace = new bool();
            IsSpace = false;
            float MolesAll = new float();
            MolesAll = 0f;
            float Temperature = new float();
            Temperature = 0f;
            int Count = new int();
            Count = 0;
            float KeyMixWorkedOut = new float();
            float Pressure = new float();







            foreach (Tuple<int, int> TileInWorkList in AdjacentTilesAndItself)
            {
                if (Globals.Airbools[TileInWorkList]["Space"] == true)
                {
                    IsSpace = true;
                    break;
                }
                if (Globals.Airbools[TileInWorkList]["Obstructed"] == false)
                {
                    foreach (KeyValuePair<string, float> KeyValue in Globals.AirMixes[TileInWorkList])
                    {
                        float MixCalculationDictionarykey = new float();
                        float JMCalculationDictionarykey = new float();
                        if (MixCalculationDictionary.TryGetValue(KeyValue.Key, out MixCalculationDictionarykey))
                        { }
                        else
                        {
                            MixCalculationDictionarykey = 0;
                        }
                        if (JMCalculationDictionary.TryGetValue(KeyValue.Key, out JMCalculationDictionarykey))
                        { }
                        else
                        {
                            JMCalculationDictionarykey = 0;
                        }

                        MixCalculationDictionary[KeyValue.Key] = (KeyValue.Value + MixCalculationDictionarykey); //*************** 
                        JMCalculationDictionary[KeyValue.Key] = ((Globals.Air[TileInWorkList]["Temperature"] * KeyValue.Value) * Globals.HeatCapacityOfGases[KeyValue.Key]) + JMCalculationDictionarykey; //***************
                        if (KeyValue.Value < 0.000000000000001)
                        {
                            keyToDelete.Add(KeyValue.Key);
                        }
                    }
                    if (0.01f < (Math.Abs(Globals.Air[Tile]["Pressure"] - Globals.Air[TileInWorkList]["Pressure"])))
                    {
                        Decay = false;
                    }
                    Count += 1;
                    TileWorkingOn.Add(TileInWorkList);
                }
            }
            if (IsSpace == false)
            {
                foreach (KeyValuePair<string, float> KeyValue in MixCalculationDictionary)
                {
                    KeyMixWorkedOut = (MixCalculationDictionary[KeyValue.Key] / Count);
                    MolesAll += KeyMixWorkedOut;

                    JMCalculationDictionary[KeyValue.Key] = (((JMCalculationDictionary[KeyValue.Key] / Globals.HeatCapacityOfGases[KeyValue.Key]) / KeyMixWorkedOut) / JMCalculationDictionary.Count);
                    Globals.AirMixes[Tile][KeyValue.Key] = KeyMixWorkedOut;
                    Temperature += (JMCalculationDictionary[KeyValue.Key] / Count);

                    if (KeyValue.Key == "Plasma")
                    {
                        if (KeyValue.Value > 0.0f) // This needs tweaking to find what the minimum amount of plasma is needed For a reaction is
                        {
                            Globals.TilesWithPlasmaSet.Add(Tile);
                        }
                        //Globals.TilesWithPlasmaSet.Add(Tile)
                    }
                }
                Pressure = (((MolesAll * Globals.GasConstant * Temperature) / 2) / 1000);

                foreach (string Key in keyToDelete)
                {
                    try
                    {
                        Globals.AirMixes[Tile].Remove(Key);

                    }
                    catch (KeyNotFoundException) { }
                }
                foreach (Tuple<int, int> TileApplyingList in TileWorkingOn)
                {
                    if (Globals.Airbools[TileApplyingList]["Space"] == false)
                    {
                        Globals.AirMixes[TileApplyingList] = new Dictionary<string, float>(Globals.AirMixes[Tile]);
                        Globals.Air[TileApplyingList]["Temperature"] = Temperature;
                        Globals.Air[TileApplyingList]["Moles"] = MolesAll;
                        Globals.Air[TileApplyingList]["Pressure"] = Pressure;
                    }
                }
            }
            else
            {
                foreach (Tuple<int, int> TileApplyingList in TileWorkingOn)
                {
                    Globals.AirMixes[TileApplyingList] = new Dictionary<string, float>(Globals.SpaceMix);

                    Globals.Air[TileApplyingList]["Temperature"] = 2.7f;
                    Globals.Air[TileApplyingList]["Moles"] = 0.000000000000281f;
                    Globals.Air[TileApplyingList]["Pressure"] = 0.000000316f;
                }
            }
            if (Decay == true)
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

            bool RemoveTile = new bool();
            foreach (Tuple<int, int> TileCalculating in Globals.UpdateTileSet)
            {
                RemoveTile = false;
                if (Globals.Lag == true)
                {
                    if (OddEven == true)
                    {
                        if (Globals.OddSet.Contains(TileCalculating))
                        {
                            RemoveTile = GasMoving(TileCalculating);
                            if (RemoveTile == true)
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
                            if (RemoveTile == true)
                            {
                                TilesRemoveFromUpdate.Add(TileCalculating);
                            }
                        }
                    }
                }
                else
                {
                    //RemoveTile = GasMoving(TileCalculating);
                    if (RemoveTile == true)
                    {
                        TilesRemoveFromUpdate.Add(TileCalculating);
                    }
                }
            }
            foreach (Tuple<int, int> TileRemoveing in TilesRemoveFromUpdate)
            {
                List<int> TileRemoveingList = new List<int>()
                {TileRemoveing.Item1, TileRemoveing.Item2};
                Globals.EdgeTiles.Add(TileRemoveingList);
                Globals.UpdateTileSet.Remove(TileRemoveing);
            }
            if (OddEven == true)
            {
                OddEven = false;
            }
            else
            {
                OddEven = true;
            }

            return OddEven;
        }



        static void DoTheEdge()
        {
            if (!Globals.EdgeTiles.Any())
            {
                return;
            }
            bool Decay = new bool();
            List<List<int>> NewEdgeTiles = new List<List<int>>();
            int CountForUpdateSet = new int();
            foreach (List<int> TileCheckingList in Globals.EdgeTiles)
            {
                Tuple<int, int> TileChecking = Tuple.Create<int, int>(TileCheckingList[0], TileCheckingList[1]);
                //Console.WriteLine(TileChecking);
                List<Tuple<int, int>> AdjacentTilesAndItself = new List<Tuple<int, int>>(Globals.DictionaryOfAdjacents[TileChecking]);
                if (AdjacentTilesAndItself.Any())
                {


                    AdjacentTilesAndItself.RemoveAt(0);
                    Decay = true;
                    CountForUpdateSet = 0;
                    foreach (Tuple<int, int> AdjacentTileTuple in AdjacentTilesAndItself)
                    {
                        
                        

                            //Console.Write(AdjacentTileTuple);
                            if (Globals.Airbools[AdjacentTileTuple]["Obstructed"] == false)
                            {
                                if (0.00001f < (Math.Abs(Globals.Air[AdjacentTileTuple]["Pressure"] - Globals.Air[TileChecking]["Pressure"])))
                                {
                                    Decay = false;
                                    if (!Globals.UpdateTileSet.Contains(AdjacentTileTuple))
                                    {
                                        List<int> AdjacentTileList = new List<int>()
                                    {AdjacentTileTuple.Item1, AdjacentTileTuple.Item2};
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
                    if (Decay == true)
                    {
                        if (Globals.CheckCountDictionary[TileChecking] >= 0)
                        {
                            bool Decayfnleall = new bool();
                            Decayfnleall = true;
                            foreach (Tuple<int, int> AdjacentTileTuple in AdjacentTilesAndItself)
                            {
                                if (Globals.UpdateTileSet.Contains(AdjacentTileTuple))
                                {
                                    Decayfnleall = false;
                                }
                            }
                            if (Decayfnleall == false)
                            {

                                if (Globals.Airbools[TileChecking]["Obstructed"] == false)
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
                    }
                    //else
                    //{
                    //    if (CountForUpdateSet > 1)
                    //    {
                    //        Globals.UpdateTileSet.Add(AdjacentTileTuple);
                    //    }
                    //{
                }
                
            }
            
            Globals.EdgeTiles = new List<List<int>>(NewEdgeTiles);
        }
        static void AirReactions()
        {
            HashSet<Tuple<int, int>> TilesWithPlasmaSetCopy = new HashSet<Tuple<int, int>>(Globals.TilesWithPlasmaSet);

            foreach (Tuple<int, int> TilePlasma in Globals.TilesWithPlasmaSet)
            {
                float AirMixesyPlasmakey = new float();
                if (Globals.AirMixes[TilePlasma].TryGetValue("Plasma", out AirMixesyPlasmakey))
                { }
                else
                {
                    AirMixesyPlasmakey = 0;
                }
                if (AirMixesyPlasmakey > 0)
                {
                    if (Globals.Air[TilePlasma]["Temperature"] > 1643.15)
                    {

                        float AirMixesyOxygenkey = new float();
                        if (Globals.AirMixes[TilePlasma].TryGetValue("Oxygen", out AirMixesyOxygenkey))
                        { }
                        else
                        {
                            AirMixesyOxygenkey = 0;
                        }
                        float AirMixesyCarbonDioxidekey = new float();
                        if (Globals.AirMixes[TilePlasma].TryGetValue("Carbon Dioxide", out AirMixesyCarbonDioxidekey))
                        { }
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
                            TemperatureScale = (Globals.Air[TilePlasma]["Temperature"] - 373) / (1643.15f - 373);
                        }
                        if (TemperatureScale > 0)
                        {
                            float PlasmaBurnRate = new float();
                            float ThePlasmaMoles = Globals.AirMixes[TilePlasma]["Plasma"];
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

                                Globals.AirMixes[TilePlasma]["Oxygen"] = TheOxygenMoles;
                                Globals.AirMixes[TilePlasma]["Plasma"] = ThePlasmaMoles;
                                Globals.AirMixes[TilePlasma]["Carbon Dioxide"] = TheCarbonDioxideMoles;

                                JM = ((Globals.Air[TilePlasma]["Temperature"] * TheCarbonDioxideMoles) * Globals.HeatCapacityOfGases["Carbon Dioxide"]);
                                J = (Globals.MolarMassesOfGases["Carbon Dioxide"] * JM);
                                J += EnergyReleased;
                                JM = (J / Globals.MolarMassesOfGases["Carbon Dioxide"]);
                                Globals.Air[TilePlasma]["Temperature"] = ((JM / Globals.HeatCapacityOfGases["Carbon Dioxide"]) / TheCarbonDioxideMoles);
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
            bool OddEven = new bool();
            OddEven = true;
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
            Console.WriteLine("Count:{0} ", Globals.UpdateTileSet.Count);
            Console.WriteLine("Count:{0} ", Globals.EdgeTiles.Count);
        }

    }
}