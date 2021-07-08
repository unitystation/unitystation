# Building and Testing

Before you PR for UnityStation, it would be helpful if you fully test your PR, read the contribution guidelines and read the [Starting Contribution](Starting-contribution.md) article on the wiki.

Below you can read our standardised multiplayer test-setup.

To test network interactions, you should set up a realistic environment. 
That means two instances: Client and Host (Server that's client, too).

You can run one instance from editor by pressing play button, 
but for another one you'll have to make a **standalone build**.

You can launch the game directly into the current scene, if it is a MainStation scene, that is open.

### TLDR QuickBuild
`Tools > Quick Build`

![quickbuild window](https://i.imgur.com/XQMuiAa.png)

- Automatically sets build settings to get MVB (minimum viable build) by including only necessary scenes.
- Operates independently of the main build window.
- Settings are persistent and won't be picked up by git (except Quick Load which is handled externally, so be sure to not commit that change).
- The Disable Scenes tab is the old `DisableNonEssentialScenes` tool.

### Initial setup: Build settings
To make a proper standalone build, you should set up build settings (only once).

* Go to `File > Build Settings`:

![](https://camo.githubusercontent.com/b2be111d41c3898d0efb0255e0878c5e3e2cc4ae/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f525554726f665a46517a79784d6851396a4d784779412e706e67)

* Make sure that **StartUp**, **Lobby**, **OnlineScene** and all MainStation scenes possible for lowpop (**Fallstation**, **SquareStation**) are checked (screenshot is out of date):

![](https://camo.githubusercontent.com/8fb35c8c3a3c25b6fa3e59231a51aefb50e18f76/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f316d4a6f7041563652476d524c5f5034525a4b374f672e706e67)

(If you don't see your current scene in the list, press `Add Open Scenes` button)

If you have **Quick Load** checked on `GameManager` prefab, you can uncheck all the asteroid and additional scenes, too.

* Mark `Development Build` and `Script Debugging` checkboxes, that will show all Build's logs in Editor's console:
![](https://camo.githubusercontent.com/ef278b53bbd024b95a20f07cb59cc015b03fee46/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f4c682d326c6542785377364148565f58636b384f64412e706e67)

* Press `Build And Run`. 

![](https://camo.githubusercontent.com/59262f9c9d0e5f74fb378419d8ae5f0ebf36346d/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f7269496b4a4759325265367657477a5a6f444d6670412e706e67)

First time you'll get a folder selection dialog where the build files will be stored. 
Create a separate folder outside of unitystation's project hierarchy. 
Then building will start, Unity will freeze until it's finished. 

![](https://camo.githubusercontent.com/fbe064882be6188ea8742a09f055725e5b010fbb/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f5f564445454758745448366c6c6e3447746d514746512e706e67)

When it's ready a game window will pop up. Now you can enjoy smooth framerates and drag window around, also launch several instances if required (that doesn't work for OSX).

Notice how Build's logs appear in Editor's console:

![](https://camo.githubusercontent.com/18d5f7dbca42d8f758740655824fb7186b718e21/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f554e306f31307650545a65573679517071614d6746772e706e67)

Next time you'll just have to press `Ctrl+B`/`Cmd+B` to Build And Run.

Important note:
> When you change stuff/recompile you'll need to **rebuild** to make Editor compatible with the Build.

### Set up ping simulation
To simulate network latency you need to select `Network Manager` in Hierarchy tab:

![](https://camo.githubusercontent.com/d1ad8c261e62e79959f45571a65a168a4a50652c/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f4572704e3378363253664b336263547a736a45355a772e706e67)

Then find `Custom Network Manager` script in Inspector tab and tick `Use network simulation`:

![](https://camo.githubusercontent.com/aae8a314f2a7e5f24328f2b7f951be27400fb168/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f396c6b6e415a6d6d523032316757387043654e6873512e706e67)

Set up desirable ping (200 is recommended) and save (so that you don't lose that setting next time Unity crashes)!

![](https://camo.githubusercontent.com/1c7484fe07642072e67e509965d399d0ca536ef2/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f5147583147736257547871784f6c454f504b733377672e706e67)

Note that actual ping will be more than expected, `200 ping` setting usually makes it 250-750

### Start up
In general it's usually like this: Build And Run, then start up game in Editor when it finishes building.
Doesn't matter much who should host and who should join, but keep in mind that Build performs better.

![](https://camo.githubusercontent.com/9722206f5fb9f62610c9b2d0821efcdece9eb7fc/68747470733a2f2f696d6167652e70726e747363722e636f6d2f696d6167652f655f33674d706a4d517a5f63753830314138664878672e706e67)

## Advanced Pre-Release Test sequence
With above testing method you can find most issues related to your code, without spending too much time building. However, before a release or when a stricter test regime is warranted, for ex. when handling sync vars and clients joining, we have an advanced test-sequence:

1. Create 2 clients (one of which hosting) with a ping of 200.
2. Play the game, chat a little, kill a little, drop things, open things and move things.
3. (extra for release) Before a release, we should test all issues and PR's that are ready for quality assessment in the release project. Test the features and bugs in every one of them, with multiple clients.
4. Join with a third 200Ping client
5. Look if your view is the same as the other clients. if not, you have found a bug already!
6. play for a while with all three
6. (extra for release) Before a release, we should test all issues and PR's that are ready for quality assessment in the release project. Test the features and bugs in every one of them, with the third client.
8. No errors or inconsistencies? great, your build just passed our Quality assessment.

Happy spess testing!

### Testing custom maps in Multiplayer
1. Make sure you are on Lobby scene
2. Set Online scene in NetworkManager (under Managers prefab):

![](https://camo.githubusercontent.com/e66b9088e6b78ae3edc2b18787be4560c86fc263/68747470733a2f2f63646e2e646973636f72646170702e636f6d2f6174746163686d656e74732f3331323435343638343032313632303733362f3439373333393936333037313739313130342f756e6b6e6f776e2e706e67)

3. Don't forget to include the scene you are testing in build preferences as well:

![](https://camo.githubusercontent.com/b77b685b1eff9f9b12860435f367bcbff88d39db/68747470733a2f2f63646e2e646973636f72646170702e636f6d2f6174746163686d656e74732f3331323435343638343032313632303733362f3439373334303639303432333038373130342f756e6b6e6f776e2e706e67)

And then Build and Run + fire up game in editor
