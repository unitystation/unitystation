# Introduction to [Multiplayer Playmode](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@0.1/manual/index.html)

Introduced in the middle of [#10052](https://github.com/unitystation/unitystation/pull/10052), Multiplayer playmode is an experimental feature by Unity that aims to make multiplayer development quicker and increase productivity for developers. 

Enabling this feature allows you to launch up to 4 clients within the editor without rebuilding the whole game from scratch.


## How to Enable MPM:

1. Go to window > Multiplayer Play Mode.
2. Activate Player 2.
3. Wait for Unity to build a library for MPM. (This process can take up to an hour.)

If you see Player 2 marked as active with a green dot, You're done!

Whenever you enter playmode now, Unity will automatically start a new instance for you to test multiplayer in and It will behave similarly to how clients work outside of the editor*


## Notes:

* Since Unity still compiles everything with editor flags, code under the `#IF UNITY_ENGINE` and `Application.IsEditor` will still run.
* Addressables does not work under MPM. As a result, you'll notice that audio is completely missing from instances.
* Unity will consume a lot of space off your SSD for each instance you enable. Make sure you have 30GBs of free storage before using this feature.
* In order to use MPM, you need 32GBs of RAM as each instance will use a minimum of 6GBs
* You only have to build the library once for one instance and Unity will copy it to other instances when needed.

# How to fix issues:

### Mouse inputs are not working correctly!
In your Instance's window, go to the top left corner and hit "Game" then switch to "Simulator" then switch back to "Game"

Mouse inputs will work correctly now.

### My library is corrupted!

1. In the MPM window, right click on the corrupted instance's name and click on "Show in Explorer". 
2. Delete everything
3. Re-Enable all instances.
4. Wait

If the issue persists:

1. Disable all instances
2. Right click in the asset browser and click on "Reimport all"
3. Wait
4. Re-enable only one instance (Player 2)
5. Wait

### All instances use outdated code / Hot Reload keeps breaking!

This is an issue with with MPM that cannot be fixed as of right now. Your only best bet is to restart the editor completely whenever this happens, or only work with server side code.

### Compiling is slower than usual!

Disable all instances when not using them, as Unity will keep forcing you to reload the domain every time you make a change and it will attempt to copy any new changes to all instances which can take a few seconds.

### MPM breaks after switching branches!

Avoid switching branches with any instances active in the background. 
Disable all instances and make sure they're marked as "inactive" before creating or switching to a new branch.

If Unity gets stuck at disabling an instance, shutdown Unity completely and switch to your desired branch before opening Unity again.