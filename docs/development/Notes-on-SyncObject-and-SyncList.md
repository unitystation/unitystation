
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

