# Starting Contribution

  This is a small guide for people who want to contribute but have no idea how to get started.

***

## First thing first

#### Downloading unity
- [Unity Hub](https://unity3d.com/get-unity/download) Manages your Unity installations, making it easier to upgrade the latest version or rollback to the previous
- [Unity personal edition](https://store.unity.com/download?ref=personal),
Just check the little box and download the installer. 
At some point you'll have to make an account, but can use disposable mail for that.

**Note that you should use Unity 2019.2.19 as of 08.02.2020.**

#### Downloading the repository
```bash
git clone https://github.com/unitystation/unitystation.git
```

Don't know how to use Git?

- [Here is a helpful guide on how to Git](https://github.com/unitystation/unitystation/wiki/GIT-basics)
- [Learn Git Branching](https://learngitbranching.js.org) is a "game" where you use git commands to progress to the next level

But if you wouldn't touch Git desktop with a 10 foot stick, and you just want to mess around with the current state of the project, just go ahead and download a [.ZIP of the repository](https://codeload.github.com/unitystation/unitystation/zip/develop)

![](https://image.prntscr.com/image/YUvWfH_uSwmqJnIQCEnDug.png)

For submitting contributions, Git is a must!

### Opening the repository
So now that you have downloaded unity and the repo, press _open_ and just browse to where you put it, be it through Git or the direct link.

![](https://cdn.discordapp.com/attachments/381634542911488001/388740773601869834/unknown.png)

First import will take some time.

After import is finished, you can open scenes: Select `File > Open Scene`, navigate to `Scenes` folder and open a scene (map) of your liking. 

**However!** If you just want to play the game (and not do mapping), open the **Lobby** scene. Lobby will load the default map for you automatically after entering the game and its behaviour will match up with the standalone builds.

![](https://image.prntscr.com/image/T7s9wVR7RhyXwwTxf4jEFg.png)

At the moment of writing the default map to load is OutpostDeathmatch. _30.03.2018_

Then you can press `Play` button on the `Game` tab to start the game in editor.

![](https://image.prntscr.com/image/G9xxyW59STqh14VslkpAzA.png)

***

## Now what?
Well, feeling confused after messing around with the scenes and all? well, it's now time to actually learn unity!

##### Reading other unitystation wikis
Take a look at [other wiki pages we have](../index.md)

##### Learning unity
There is a huge amount of resources around for learning Unity, and for the more experienced in the code area, it'll be a piece of cake!

[_Official Unity tutorials_](https://unity3d.com/learn/tutorials) All people new to unity start here, it'll teach you basics like the UI, the script and scene editors, and all sorts of things.

[_Official Unity manual_](https://docs.unity3d.com/Manual/index.html) The manual is very useful, even for the more experienced, always keep it close.

##### Downloading a code editor
There is a lot you can accomplish through editors available from Unity, but if you plan on contributing seriously to the code/scripts, you'll do better if you have a development environment set-up.

Editors that you might like include:

- [Visual Studio Code](https://code.visualstudio.com/)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- [Visual Studio](https://visualstudio.microsoft.com/vs/whatsnew/) 2015 or newer
- [Atom](https://ide.atom.io/)
- [Vim](https://www.vim.org/)

Regardless of which editor you choose, you'll want to install any relevant extensions from your editor's Marketplace for unity C# projects, including the "C# powered by OmniSharp" plugin if your IDE has one.

Don't forget to enable Roslyn Analyzers (static code analysis) if they are supported, and enable "Format on Save" in your Settings so your editor will automatically fix indentations and whitespace issues for you.

When opening the unitystation project using your code editor, open the "UnityProject" subfolder as your editor's workspace, instead of the root "unitystation" folder that contains the entire repo.

##### Getting help
There is no shame in asking questions.

[_Unitystation discord_](https://discord.gg/TMRMfpS) Anyone would be glad to answer your questions, just ask!
