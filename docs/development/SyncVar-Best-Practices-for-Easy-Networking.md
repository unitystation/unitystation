The [SyncVar attribute](https://mirror-networking.com/docs/Guides/Sync/SyncVars.html) can save a lot of time and code for syncing data from server to all clients (because it avoids having to implement a custom Net Message), but many of us have struggled with understanding its peculiarities (of which it has many) especially as it pertains to how Unitystation is put together. Having used it quite a bit now, I've developed a set of simple "best practices" for how to use them which should hopefully save future developers from struggling with it.

# Proper SyncVar Usage
These are things you should almost ALWAYS do if using syncvar. If you see places in the code where these rules are violated, be suspicious of bugs. Note that most of these tips only apply if you define a "hook" method.

1. Add SyncVar to the field you want to sync. The field should ALWAYS be private, and it should NOT be editor assignable. NEVER allow the field to be directly modified by other components or in editor. If you want to configure an initial value for this field, create a separate editor field for it.

        :::csharp
        public class ItemAttributes : NetworkBehavior
        {
        
            //allows configuring the initial value in editor - NOT a syncvar!
            [SerializeField]
            private string initialName;    

            //the actual syncvar
            [SyncVar(hook=nameof(SyncItemName))]
            private string itemName;
        
        }

    * If the field needs to be viewable externally, create a public readonly accessor:

            :::csharp
            public string ItemName => itemName;

    * If other components need to know when the syncvar changes, create a UnityEvent they can subscribe to which you invoke in your hook method.

            :::csharp
            [NonSerialized] // usually don't want these to be assignable in editor
            public StringEvent OnItemNameChange = new StringEvent();
            
            // event class - declare this outside of component class
            class StringEvent : UnityEvent<string> { }

3. (Only if you have a hook method) Define an EnsureInit private method and put any necessary init logic for your component in there (caching components, setting initial values) along with a check that skips the init logic if the component is already initialized. You will add this to the top of any SyncVar hook methods, OnStartClient, OnStartServer, Awake, and Start methods. This is necessary because Mirror does not guarantee that its methods will be called before Awake()/Start(), so we have to always ensure the component is initialized. Search for "EnsureInit" in our codebase for examples. For example, here we cache a spriteHandler component

        :::csharp
        private void EnsureInit()
        {
            if (!this.spriteHandler) {
            this.spriteHandler = GetComponentInChildren<SpriteHandler>();
            }
        }

2. (Only if you have a hook method) Define a private hook method named "Sync(name of field)". The line of the hook after EnsureInit should update the field based on the new value. Do NOT make a protected or public hook method. Starting the method name with Sync is important because it makes it easier for others to know that this method is exclusively for changing this syncvar.

        :::csharp
        private void SyncItemName(string oldName, string newName)
        {
            EnsureInit()
            this.itemName = newName;
            //any other logic needed goes here
        }

    * You generally will need a hook unless the client doesn't need to invoke any special logic when the value changes.
    * Note that Mirror actually does set the field automatically on the clientside when the hook is triggered by a server update, but if you called the hook directly on server side (instead of actually changing the field's value) it would not automatically change the value. This has created a lot of needless confusion and mistakes in the past because it is so situational, so sticking to the conventions documented on this page will avoid that confusion.

3. (Only if you have a hook method) Override OnStartClient (make sure to use the "override" keyword!) and invoke the hook, passing it the current value of the field. If you are extending a component, make sure to call base.OnStartClient(). This ensures the SyncVar hook is called based on the initial value of the field that the server sends. Also call EnsureInit at the top to ensure any necessary init logic is called (Mirror may call OnStartClient before Awake/Start)

        :::csharp
        //make sure to use "override" and use correct name "OnStartClient"
        public override void OnStartClient()
        {
            EnsureInit()
            SyncItemName(this.itemName);
            base.OnStartClient();
        }
        
4. (Only if you have a hook method) Implement the IServerSpawn interface and set the syncvar field to the initial value in the method. This is a method which is invoked when an object is being spawned, regardless of if it's coming from the pool or not. This ensures that the object is properly re-initialized when it is being spawned from the object pool.

        :::csharp
        public void OnSpawnServer()
        {
            //object starts with editor-configured initial name
            SyncItemName(initialName);
            
            //if extending another component
            base.OnSpawnServer();
        }
        
5. (Only if you have a hook method) The ONLY place you are allowed to change the value of the syncvar field is via the syncvar hook and only on the server! Never change the value on the client side, and never modify the field directly. If you are on the server and you want to change the field value, call the hook method and pass it the new value. This ensures that the hook logic will always be fired on both client and server side.

        :::csharp
        [Server]
        private void ServerChangeName(string newName)
        {
            //NO! BAD! DON'T DO THIS!
            this.itemName = newName;

            //YES! GOOD! DO THIS INSTEAD!
            SyncItemName(newName);
        }
        
6. Do not rely on a consistent ordering when it comes to syncvar changes and net messages sent from the server. Due to network latency, if you change 2 syncvars and send a net message on the server, the updates could arrive on the client in any order.

# Various Issues Caused by Improper SyncVar Usage
Here's some symptoms of improper syncvar usage:

1. Clients who join after the syncvar field has been updated during gameplay (sometimes called "late joining clients") will not display the correct game state. Usually caused by not calling the hook in OnStartClient, sometimes caused by failing to properly override OnStartClient (using override keyword and using the correct method name).

2. Server player sees a different game state than the client. Usually caused by setting the syncvar field directly without going through the hook method.

3. SyncVar hooks throwing NREs - usually caused by failing to define an EnsureInit() method and call it at the top of the SyncVar hooks. Unity is calling the SyncVar hook before Awake/Start.

4. Client intermittently sees the incorrect game state related to a sync var. Usually caused by having SyncVar logic which has a race condition - the client is relying on the syncvar updates / server messages arriving in a particular order.

5. A newly-spawned object behaves differently than other objects of its type. This is due to failing to have a separate editor-assignable initial value for the field or failing to reset the field in OnSpawnServer. Your SyncVar field value was set to something other than the default, then was destroyed and returned to pool, then spawned from the pool with that same field value. This is solved by implementing IServerSpawn and initializing the syncvar value to its proper initial value (either the editor-assignable value or a hardcoded default).

# Surprising SyncVar Facts
Here are some of the things you might not know about how they work which explains the reasoning for the above practices. Not 100% certain on all of these but they might as well be true.

1. SyncVar hooks don't fire on the server when the server changes the field. They only fire on the client when the client receives the new value. Thus the server player will not have the hook called. This is the reason to always change the syncvar by way of the hook method on the server.

2. SyncVar hooks don't fire when client connects and receives the initial value of the field. This is why you should call the syncvar hook manually in OnStartClient.

3. SyncVar only works properly on specific types of fields ([see the docs](https://docs.unity3d.com/Manual/UNetStateSync.html)), but Unity does not seem to complain very noisily if you use it on an invalid type - the game still builds. If your syncvar doesn't appear to be working, chances are you are using it on an invalid type.

4. SyncVar hooks, OnStartServer and OnStartClient may be called before Unity's Awake/Start hooks.

5. If you don't update the field's value in the syncvar hook using the new value passed to the hook method, the client-side value won't be updated. This is why you should always update the field in the first line of the hook method.

6. It takes a varying amount of time for the server to send the updated SyncVar field to the client, so make sure the code doesn't depend on the syncvar change arriving at a particular time or in a particular order in relation to other network messages or syncvar updates. If you change 2 syncvars on the server and also send a net message, they could arrive on the client in any order.
