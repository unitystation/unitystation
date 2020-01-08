# Simulating Lag
This page shows how to simulate lag / high ping situations when playing locally or connecting to a remote server. The approach is different depending on your operating system and version, so please refer to the appropriate section for your system. Please update this page if you run into issues and remove "not yet tested" if you found it to work.

It's important to test with lag so that you can ensure the game still operates smoothly when someone doesn't have a great connection to the server. It's one of the key capabilities that helps the new remakes stand out from BYOND.

When testing locally, **make sure you are connecting to 127.0.0.1 instead of localhost** (not sure if this is necessary on all OSs but it's necessary on OSX). Otherwise, most of these tools should work just fine when connecting to a remote server.

### Windows
NOTE: Not yet tested.

[Clumsy](https://jagt.github.io/clumsy/). 

### OSX
They took away IPFW, so it and several tools that depended on it no longer work. We wrote a script to use the new replacement for IPFW - pfctl / dnctl. Ignore any warnings about ALTQ. Apple decided to be special and not implement the full feature set of these tools so we have to resort to undocumented workarounds.

Open a terminal in the unitystation/UnityProject folder (this is the default location if you use the Terminal tab in Rider).

Start lagging with:
```bash
sudo ./lag.sh
```

Stop lagging with:
```bash
sudo ./stoplag.sh
```

By default it adds 300 ms of latency. You can override this using an argument. This example shows how to set 500 ms of latency:
```bash
sudo ./lag.sh 500
```

Several sites suggest Network Link Conditioner. However, THIS DOESN'T WORK FOR localhost or 127.0.0.1, so it's pretty useless!

### Linux
NOTE: Not yet tested.

[netem](https://bencane.com/2012/07/16/tc-adding-simulated-network-latency-to-your-linux-server/)

If you found a good approach for using netem, please check in some .sh scripts to easily turn it on and off (see lag.sh and stoplag.sh for reference of how they can work).


