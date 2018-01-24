using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;



namespace Atmospherics {
 class MainClass {
  public static void Main(string[] args) {
   Console.WriteLine("Hello World!");
   Console.WriteLine("Press any key to continue . . . ");
   Console.ReadKey();

   List < int > Tile = new List < int > ();
   Tile.Add(20);
   Tile.Add(20);
   //GenerateAdjacentTiles(Tile);





   Console.WriteLine("Press any key to continue . . . ");
   Console.ReadKey();
  }


 }
 class Initialization {
  static void AirAndOtheresEncahon() {
   Dictionary < List < int > , Dictionary < string, float >> DictionaryOfAdjacents = new Dictionary < List < int > , Dictionary < string, float >> ();
  }
  static List < List < int >> GenerateAdjacentTiles(List < int > Tile)

  {
   List < int > TileRange = new List < int > ();
   TileRange.Add(360);
   TileRange.Add(210);

   List < List < int >> AdjacentTilesRelativeCoordinatesList = new List < List < int >> ();

   List < int > temporaryList = new List < int > () {
    0,
    0
   };
   AdjacentTilesRelativeCoordinatesList.Add(temporaryList);

   List < int > temporaryList1 = new List < int > () {
    1,
    0
   };
   AdjacentTilesRelativeCoordinatesList.Add(temporaryList1);

   List < int > temporaryList2 = new List < int > () {
    0,
    1
   };
   AdjacentTilesRelativeCoordinatesList.Add(temporaryList2);

   List < int > temporaryList3 = new List < int > () {
    -1, 0
   };
   AdjacentTilesRelativeCoordinatesList.Add(temporaryList3);

   List < int > temporaryList4 = new List < int > () {
    0,
    -1
   };
   AdjacentTilesRelativeCoordinatesList.Add(temporaryList4);

   List < List < int >> WorkedOutList = new List < List < int >> ();
   foreach(List < int > TileOffset in AdjacentTilesRelativeCoordinatesList) {
    List < int > subList = new List < int > ();

    int WorkedOutOffset1 = TileOffset[0] + Tile[0];
    subList.Add(WorkedOutOffset1);
    int WorkedOutOffset2 = TileOffset[1] + Tile[1];
    subList.Add(WorkedOutOffset2);

    if (!(subList[0] > TileRange[0] || subList[0] < 0 || subList[1] > TileRange[1] || subList[1] < 0)) {
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
  static void MakingDictionaryOfAdjacents() {
   Dictionary < List < int > , List < List < int >>> DictionaryOfAdjacents = new Dictionary < List < int > , List < List < int >>> ();
   List < int > TileRange = new List < int > ();
   TileRange.Add(360);
   TileRange.Add(210);
   IEnumerable < int > nuberelist = Enumerable.Range(0, (TileRange[0] + 1));
   IEnumerable < int > nuberelist2 = Enumerable.Range(0, (TileRange[0] + 1));
   foreach(int nubere in nuberelist) {
    foreach(int nubere2 in nuberelist2) {
     List < int > newListcontes = new List < int > {
      nubere,
      nubere2
     };
     List < List < int >> ageints = GenerateAdjacentTiles(newListcontes);
     DictionaryOfAdjacents.Add(newListcontes, ageints);
    }
   }

   Console.WriteLine("MakingDictionaryOfAdjacents Done!");
  }
  static void PitchPatch() {
   HashSet < List < int >> odd_set = new HashSet < List < int >> ();
   HashSet < List < int >> Even_set = new HashSet < List < int >> ();

   List < int > TileRange = new List < int > ();
   TileRange.Add(360);
   TileRange.Add(210);
   IEnumerable < int > nuberelist = Enumerable.Range(0, (TileRange[0] + 1));
   IEnumerable < int > nuberelist2 = Enumerable.Range(0, (TileRange[0] + 1));
   foreach(int nubere in nuberelist) {
    foreach(int nubere2 in nuberelist2) {
     if (nubere % 1 == 0) {
      if (nubere2 % 1 == 0) {
       List < int > newListcontes = new List < int > {
        nubere,
        nubere2
       };
       odd_set.Add(newListcontes);
      } else {
       List < int > newListcontes = new List < int > {
        nubere,
        nubere2
       };
       Even_set.Add(newListcontes);
      }
     } else {
      if (nubere2 % 1 == 0) {
       List < int > newListcontes = new List < int > {
        nubere,
        nubere2
       };
       Even_set.Add(newListcontes);
      } else {
       List < int > newListcontes = new List < int > {
        nubere,
        nubere2
       };
       odd_set.Add(newListcontes);
      }
     }

    }
   }
   Console.WriteLine("PitchPatch Done!");
  }
  static void MakingCheckCountDictionarys() {
   Dictionary < List < int > , int > CheckCountDictionary = new Dictionary < List < int > , int > ();
   Dictionary < List < int > , int > CheckCountDictionaryMoving = new Dictionary < List < int > , int > ();

   List < int > TileRange = new List < int > ();
   TileRange.Add(360);
   TileRange.Add(210);
   IEnumerable < int > nuberelist = Enumerable.Range(0, (TileRange[0] + 1));
   IEnumerable < int > nuberelist2 = Enumerable.Range(0, (TileRange[0] + 1));
   foreach(int nubere in nuberelist) {
    foreach(int nubere2 in nuberelist2) {
     List < int > newListcontes = new List < int > {
      nubere,
      nubere2
     };
     CheckCountDictionary.Add(newListcontes, 0);
     CheckCountDictionaryMoving.Add(newListcontes, 0);
    }
   }
   Console.WriteLine("MakingCheckCountDictionarys Done!");
  }


 }

}
