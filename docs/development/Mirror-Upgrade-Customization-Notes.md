# Mirror Upgrade Customization Notes

This documents the customizations we've done on Mirror which will need to be manually upgraded when we upgrade mirror.

~Fixed telepathy shutdown in NetworkManager
https://github.com/unitystation/unitystation/pull/2154/commits/c84b973827ab9f67ef9ae77954ae6e5a7de86653~ This will be moved to CustomNetworkManager in my upcoming mirror upgrade PR so it's external to mirror.

~Looks Like something needed to be fixed in NetworkReader also: https://github.com/unitystation/unitystation/pull/2154/commits/fe5dbb949ba263198dbfd7ce8ebe43be3a5bd5d8~ Nope, it was just adding an unnecessary semicolon.

~changes to clientscene and Runtime/Messages: https://github.com/unitystation/unitystation/pull/2154/commits/2517cb7224bd95375f8dcfa0c36fe99405e8b379~
Disregard, was reverted

I manually patched in this fix until it is included in the latest mirror asset store release:
https://github.com/vis2k/Mirror/commit/0e1bc8110fb3cc4e162464a2e080eac6c70ab95e
Once asset store release is upgraded we can switch to it and overwrite this fix.