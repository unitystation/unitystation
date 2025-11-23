using System.Collections.Generic;
using Logs;
using Newtonsoft.Json;
using SecureStuff;

namespace Managers.Supporters.FetchSupporterMethods
{
    public class FetchSupportersFromJsonInStreamingAssets : IFetchSupporters
    {
        private const string FileName = "supporters.json";

        public List<Supporter> FetchSupporters()
        {
            try
            {
                if (AccessFile.Exists(FileName, true, FolderType.Data))
                {
                    string jsonContent = AccessFile.Load(FileName, FolderType.Data);
                    return JsonConvert.DeserializeObject<List<Supporter>>(jsonContent) ?? new List<Supporter>();
                }

                // If the file does not exist, create a default one
                var defaultSupporters = new List<Supporter>();
                defaultSupporters.Add( new Supporter
				{
					Identifier = "InsertAccountIdHere",
				});
                string defaultJson = JsonConvert.SerializeObject(defaultSupporters, Formatting.Indented);
                AccessFile.Save(FileName, defaultJson, FolderType.Data);
                Loggy.Info("Created default supporters file.");

                return defaultSupporters;
            }
            catch (System.Exception ex)
            {
                Loggy.Error($"Error handling supporters file: {ex.Message}");
                return new List<Supporter>();
            }
        }
    }
}