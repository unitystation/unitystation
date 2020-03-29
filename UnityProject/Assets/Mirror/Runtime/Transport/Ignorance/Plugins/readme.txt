ENET Pre-compiled Binary Library Blobs
==========================
This folder contains a set of compiled binary blobs from https://github.com/SoftwareGuy/ENet-CSharp .
These are NOT compatible with upstream's ENET.

Contained within these folders lies the following:

* iOS
- Universal for iOS 8.x+
* Android
- arm64-v8a: AArch64/ARM64 ENET binary.
- armeabi-v7a: ARMv7 ENET binary.
- x86: 32Bit x86 Android ENET Binary.
- NOTE: Minimum of Android KitKat 4.4 OS required.
* Windows
- enet.dll: Windows 64-bit, compiled using Visual Studio 2019.
* MacOS
- libenet.bundle: MacOS compiled ENET Binary using Apple CLang from XCode. (CMake & Make)
* Linux
- libenet.so: Ubuntu 18.04 compiled ENET Shared Binary.

COMPILE THE CODE YOURSELF
=========================
If you don't trust the above binaries then git clone the repository and read the readme. We use MSBuild for max awesomeness.
iOS and Android compiles require additional work.

EXCLUSION INSTRUCTIONS
======================
No need, the meta data will cover that for you.

Still don't know what to do with these? Drop by the Mirror discord and post in the #ignorance channel.