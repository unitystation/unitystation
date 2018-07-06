using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace Chemistry
{

    internal class MainClass
    {
        public static void Main(string[] args)
        {
            Initialization.run();
            //Demonstration code//
            var area = new Dictionary<String, float>();
            area.Add("potassium", 59.0f);
            area.Add("oxygen", 49.0f);
            area.Add("sugar", 45.0f);
            area.Add("iodine", 20.0f);

            //area.Add("virusfood",59f);
            //area.Add("blood",49f);
            float Temperature = 400f;
            Dictionary<string, float> area_new = Calculations.Reactions(area,Temperature);

            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey();
        }
        public class Reaction
        {
            public String Name { get; set; }
            public Dictionary<String, float> Results { get; set; }
            public Dictionary<String, float> Reagents_and_ratio { get; set; }
            public Dictionary<String, float> Catalysts { get; set; }
            public float Minimum_temperature { get; set; }
        }
        public static class Globals
        {
            public static Dictionary<String, HashSet<Reaction>> Reactions_store_Dictionary = new Dictionary<String, HashSet<Reaction>>();
            public static List<Reaction> List_of_reactions = new List<Reaction>();
        }

        private static class Initialization
        {
            public static void run()
            {
                Initialization.JsonImportInitialization();
                Initialization.CemInitialization();
            }
            private static void JsonImportInitialization()
            {
                string json = File.ReadAllText(@"Reactions.json");
                var Json_Reactions = JsonConvert.DeserializeObject<List<Dictionary<String,Object>>>(json);
                for (var i = 0; i < Json_Reactions.Count() ; i++)
                {
                    Reaction Reaction_pass = new Reaction();
                    Reaction_pass.Name = Json_Reactions[i]["Name"].ToString();
                    Reaction_pass.Results = JsonConvert.DeserializeObject<Dictionary<string, float>>(Json_Reactions[i]["Results"].ToString());
                    Reaction_pass.Reagents_and_ratio = JsonConvert.DeserializeObject<Dictionary<string, float>>(Json_Reactions[i]["Reagents_and_ratio"].ToString());
                    Reaction_pass.Catalysts = JsonConvert.DeserializeObject<Dictionary<string, float>>(Json_Reactions[i]["Catalysts"].ToString());
                    Reaction_pass.Minimum_temperature = float.Parse(Json_Reactions[i]["Minimum_temperature"].ToString());
                    Globals.List_of_reactions.Add(Reaction_pass);
                }
                //Console.WriteLine("JsonImportInitialization done!");
            }
            private static void CemInitialization()
            {
                for (var i = 0; i < Globals.List_of_reactions.Count(); i++)
                {
                    
                    foreach (string Chemical in Globals.List_of_reactions[i].Reagents_and_ratio.Keys)
                    {
                        if (!(Globals.Reactions_store_Dictionary.ContainsKey(Chemical)))
                        {
                            Globals.Reactions_store_Dictionary[Chemical] = new HashSet<Reaction>();
                        }
                        Globals.Reactions_store_Dictionary[Chemical].Add(Globals.List_of_reactions[i]);
                    }
                }
            }
        }
        public static class Calculations
        {
            public static Dictionary<string, float> Reactions(Dictionary<string, float> Area, float Temperature)
            {
                HashSet<Reaction> Reaction_buffer = new HashSet<Reaction>();
                foreach (string Chemical in Area.Keys)
                {
                    foreach (var Reaction in Globals.Reactions_store_Dictionary[Chemical])
                    {
                        if (!(Reaction.Minimum_temperature > Temperature))
                        {
                            bool Valid_Reaction = new bool();
                            Valid_Reaction = true;
                            foreach (string Required_chemical in Reaction.Reagents_and_ratio.Keys)
                            {
                            if (!(Area.ContainsKey(Required_chemical)))
                                {
                                Valid_Reaction = false;
                                }
                                
                            }
                            if (Valid_Reaction)
                            {
                                Reaction_buffer.Add(Reaction);
                            }
                        }
                    }
                }
                foreach (var Reaction in Reaction_buffer)
                {
                    List<string> Compatible_chemical = new List<string>();
                    foreach (string Chemical in Reaction.Reagents_and_ratio.Keys)
                    {
                        bool Compatible = new bool();
                        Compatible = true;
                        foreach (string Sub_Chemical in Reaction.Reagents_and_ratio.Keys)
                        {
                            if (Area[Chemical] * (Reaction.Reagents_and_ratio[Sub_Chemical] / Reaction.Reagents_and_ratio[Chemical]) > Area[Sub_Chemical])
                            {
                                Compatible = false;
                            }
                        }
                            if (Compatible)
                            {
                                Compatible_chemical.Add(Chemical);
                            }
                    }
                    if (Compatible_chemical.Any())
                    {
                        var Compatible_cem = Compatible_chemical[0];
                        var back_up = Area[Compatible_cem];
                        foreach (string Chemical in Reaction.Reagents_and_ratio.Keys)
                        {
                            if (!(Reaction.Catalysts.ContainsKey(Chemical)))
                            {
                                Area[Chemical] = (Area[Chemical] - Area[Compatible_cem] * (Reaction.Reagents_and_ratio[Compatible_cem] / Reaction.Reagents_and_ratio[Chemical]));
                            }
                        }
                        foreach (string Chemical in Reaction.Reagents_and_ratio.Keys)
                        {
                            if (!(Area[Chemical] > 0))
                            {
                                Area.Remove(Chemical);
                            }
                        }
                        foreach (string Chemical in Reaction.Results.Keys)
                        {
                            float Chemical_amount = 0;
                            if (Area.ContainsKey(Chemical))
                            {
                                Chemical_amount = Area[Chemical];
                            }
                            Area[Chemical] = Chemical_amount + Reaction.Results[Chemical] * back_up / Reaction.Reagents_and_ratio[Compatible_cem];
                        }
                    }
                }
                return (Area);
            }
        }
    }
}