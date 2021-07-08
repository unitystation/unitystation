# Mirror Upgrade Customization Notes

This documents the customizations we've done on Mirror which will need to be manually upgraded when we upgrade mirror.

On 22/02/2021 Mirror was updated to version 30.5.3 by this PR https://github.com/unitystation/unitystation/pull/6083

All the custom unitystation code in the mirror code should be labled by : //CUSTOM UNITYSTATION CODE//

I manually patched in this fix until it is included in the latest mirror asset store release:
https://github.com/vis2k/Mirror/commit/0e1bc8110fb3cc4e162464a2e080eac6c70ab95e
Once asset store release is upgraded we can switch to it and overwrite this fix.

Increase performance with isDirty flag here: https://github.com/unitystation/unitystation/commit/ff96560277c727dc262881873e2262cf3e571850#diff-bfbd1e91b0913c114ca897e7d6b7cc62

Implement all of the changes for our custom observer system found here (search for NetworkIdentity, NetworkBehaviour and NetworkServer for the big commits):

In order from oldest to newest (oldest first):
https://github.com/unitystation/unitystation/commit/271f5da9e2fb762c10a0b4ff3fd0adb19d8deadd#diff-bfbd1e91b0913c114ca897e7d6b7cc62

https://github.com/unitystation/unitystation/commit/779a83272e1ec0744a87f0ed4fe7cbf5868e3a39#diff-bfbd1e91b0913c114ca897e7d6b7cc62

https://github.com/unitystation/unitystation/commit/de185ba2d272e7a92907d124c3e2ac09c4882a99#diff-bfbd1e91b0913c114ca897e7d6b7cc62

https://github.com/unitystation/unitystation/pull/4194/commits
