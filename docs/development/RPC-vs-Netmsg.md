When looking through our code, you'll notice different that some network actions are handles differently. We have a guideline with which one to use. But take note: although many are already in the codebase, their legacy use may be wrong in comparison to current guidelines. So don't trust the code blindly!

## NetMsg

Netmessages should be used in case where security of information is important. Ask yourself "If someone fakes this, could it influence the gameplay for others"?
Be mindfull that NetMsg does create some garbage, so use it wisely!

## RPC
RPC is a nice and clean solution to send non-secure data. An example for non-secure data is grafical-only information such as disabling a SpriteRenderer. RPC is however a clean and efficient protocol, that should be used where security is not an issue.

Please note, that some insignificant graffical updates, may be important to send sparsely.

## SyncVar
SyncVar can be a simple alternative to NetMsg or RPC for sending updates to all clients. But it has some caveats for how to use it without having unexpected behavior. See [SyncVar Best Practices for Easy Networking](SyncVar-Best-Practices-for-Easy-Networking.md)