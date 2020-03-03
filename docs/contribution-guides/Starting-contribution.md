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

### Forking the repository
So you have the repository downloaded, Unity opened, and want to help out. What do you do? Well first you will need to FORK the repository! This basically means that you are making a copy of the repository.

So first what you are going to want to do is click on the **Fork** button.
![](https://puu.sh/Fa9e6.png)

After some time passes, you will get redirected to your newly created repository! It will be named *YourGitHubUsername/unitystation* From here you can do exactly what you did with the original repository, but with the addition of being able to submit the changes you make as Pull Requests! 

## Updating the repository
After a while, other people will begin submitting their own changes that will then get pulled into the original repository that everyones fork is based off of. When this happens, your forked repository will no longer be up to date and will need to be updated!

To begin, you first will need to go back to your forked repository and click on the button that says `Compare`.
![](https://puu.sh/Fa9lc.png)

Next you will then need to click on the left most dropdown menu and click your forked repository.
![](https://puu.sh/Fa9n2.png)

After doing so, you may notice that the four dropdown menus with repositories have vanished and you are now left with two dropdown menus with branches on your own forked repository. No worry, just click on the hyperlink that says `compare across forks`
![](https://puu.sh/Fa9oJ.png)

Now just simply click the third dropdown menu and locate the original repository.
![](https://puu.sh/Fa9rJ.png)

Now you will be given the option to do a Pull Request of from *unitystation/unitystation* to *YourGitHubUsername/unitystation*. It is basically the reverse that you do when sending changes to the original repository! 

Clicking on the `pull request` button will bring you to the final step of updating, which is to merge the pull request! Simply click on the `Merge` button on the bottom of the page and you will have your fork updated to the latest change submitted!

It is very important, espescially when doing mapping, that you have your fork always updated! Changes happen everyday, so you won't want to do extra work that someone else has already done or even worse; to mess up what has been done!
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
