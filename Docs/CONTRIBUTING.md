# How to Contribute

### Note: This is for macOS. Windows instructions coming soon
***

If you have never used git before the easiest solution is to download and install the github desktop.
You can get the client here: [Download Github Desktop](https://desktop.github.com/)

Once installed, you will need to fork the unitystation repo. Forking basically means 'making a copy'. The newly forked repo will show up underneath your profile on Github. Here you can do whatever you like with the code and eventually propose changes to the main unitystation repo by submitting a Pull Request.

### To fork the repo and set it up with Github Desktop follow these steps:

***
1. Navigate to the [Unitystation repo](http://github.com/unitystation/unitystation)
2. Press the fork button located at the top right of the screen and save the fork under your github name
3. Navigate to the newly forked repo (i.e. github.com/YourUsername/unitystation)
4. At the top right of the repo, click the green 'Clone or Download' button and choose 'Open in Desktop'
5. Github Desktop should open up, here you can choose where to save the repo on your machine


***

### Important tips!

***
 
Pay attention to the Branch bar at the top of Github Desktop:

![](https://cdn.discordapp.com/attachments/304941207883087872/305286669609730049/unknown.png)

1. Top left you will notice a drop down box next to the new branch button. Make sure you are working on the correct branch within your forked repo (for the purposes of contributing to unitystation you should only be concerned with the develop branch)
2. Before you start working on your feature, make sure you update your fork first by pressing the 'Update from unitystation/develop' button. It will pull down any changes to your git from the unitystation repo. Then you need to press the Sync button located on the top right <-- This part is **Important**, this will ensure the updates are pushed to your fork on github as well as your machine. You need to remember to do this step before you start work, don't forget!
3. When working on your feature be sure to submit Pull Requests often as others may be working on the same scripts or prefabs as you. If you wait many days before submitting, you might find that your work is rejected as it is sometimes too difficult to resolve merge conflicts with .prefab and .unity files


***
### Submitting a Pull Request

***

After you have saved your changes in unity, open Github Desktop. You will notice that the Uncommited Changes table has filled with all the files you have changed in the project. Follow the steps below to commit the changes and submit a Pull Request to the main unitystation repo:

1. At the bottom of Github Desktop, type in a title for your new commit and add a description:

![](https://cdn.discordapp.com/attachments/304941207883087872/305291127546970112/unknown.png)

2. Press the Commit to develop button
3. At the top right of Github Desktop, press the Pull Request button:

![](https://cdn.discordapp.com/attachments/304941207883087872/305291398767443968/unknown.png)

4. Confirm you are happy with the description and press the Send Pull Request button
5. When that is done make sure to press the Sync button to also push the changes to your fork on Github

## Todo vs Feature vs bug:
Please take note of the difference between a TODO and Feature
* Bug: An unexpected behaviour of the game and/or server. Including, but not limited to, errors and warnings.
* Todo: When you come across something that needs tweaking/adding during development, is not an unexpected behaviour
* Feature: When you, out of personal preference, want something added or changed.

### That's it!
Someone will come along and review the changes. If everything looks good then they will merge it with the main repo. If you need any help don't be afraid to ask in the discord channel: [https://discord.gg/tFcTpBp](https://discord.gg/tFcTpBp)



