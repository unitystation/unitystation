
# Note SyncObject And SyncList Usage
If present on a network behaviour it will synchronise the current state of the object to the client, 
however due to some of our changes to optimise mirror, if you change any element or  change the SyncObject  in anyway, 
you have to manually mark the netIdentity.isDirty = true, like so

        :::csharp
        public SyncList<SceneInfo> loadedScenesList = new SyncList<SceneInfo>();
        
        public void addSceneInfo()
        {
            loadedScenesList.Add(new SceneInfo
            {
            	SceneName = serverChosenMainStation,
            	SceneType = SceneType.MainStation
            });
            netIdentity.isDirty = true;
        }
# Note OnStartClient needed to be added if you have hook Logic for SyncList
so, You need to implement OnStartClient , If you have any thing that happens because of the elements in the list,
this is due to the payload Being applied before you can subscribe to the hook,
So basically you just got to do the hooks work for it and bring up-to-date the client state


        :::csharp
		public override void OnStartClient()
		{
			addedLanguages.Callback += OnLanguageListChange;

			// Process initial SyncList payload
			for (int index = 0; index < addedLanguages.Count; index++)
				OnLanguageListChange(SyncList<NetworkLanguage>.Operation.OP_ADD, index, new NetworkLanguage(), addedLanguages[index]);
		}

