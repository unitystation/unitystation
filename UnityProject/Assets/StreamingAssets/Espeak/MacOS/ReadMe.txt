Hello, no one has compiled espeak for MacOS yet but if you have some basic
technical skills you should be able to.

1. Read https://github.com/Elijahrane/espeak-nt/blob/master/docs/building.md
2. Pull from https://github.com/Elijahrane/espeak-nt/
3. Compile with linker flags set to look in local directory.
4. Test the program yourself normally
5. Put it into this folder. The MacOS layout should roughly mirror the Linux one.
6. Get latest UnityStation and make your own fork
7. In the project, open Assets/Scripts/Chat/ChatRelay.cs
8. Uncomment the commented code in the function startEspeak
9. Remove the Mac Relevant Code in the function trySendingTTS
10. Build in Unity Editor and test in-game
11. Make sure you packaged correctly and it works on machine that haven't compiled espeak
12. Make a pull request on GitHub.
13. ???
14. Profit!
