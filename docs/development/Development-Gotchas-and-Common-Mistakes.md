### Development Gotchas and Common Mistakes

Lists of things that we expect in your PRs, and common mistakes / things to look out for when developing and testing.

#### PR Acceptance
1. Code should follow our [Development Standards](../contribution-guides/Development-Standards-Guide.md)
3. Code should compile without new errors or warnings.
4. PR should be tested in editor (most PRs should be tested hosting in editor and joining in a standalone build).
6. Any new or changed components should follow the [Component Development Checklist](Component-Development-Checklist.md)
7. Any new objects / items follow the [Creating Items and Objects Guide](Creating-Items-and-Objects.md) (especially concerning the use of prefab variants)

#### Common Issues
1. Your changes break... 
    1. when there's 2 clients (rather than just 1 host player and 1 client). Testing with a local headless server can make this more feasible if your computer can't handle running 3 full instances of the game
    2. when the round restarts (try Networking > Restart Round to quickly restart)
    2. on moving matrices or when matrices rotate
    2. when crossing matrices or engaging with the feature across matrices
2. You forgot to think about how an object should work when it is in inventory.
2. You depended on Awake() being called before mirror hooks (SyncVar hooks, OnStartClient, OnStartServer), even though Mirror doesn't guarantee that (this often is the source of NREs). See [SyncVar Best Practices](SyncVar-Best-Practices-for-Easy-Networking.md)
4. You didn't use syncvars as per our recommendations. See [SyncVar Best Practices](SyncVar-Best-Practices-for-Easy-Networking.md)

#### Debugging
1. If you're getting an error spammed in the logs, make sure to check the point where the error spamming begins to see if there's some other error that is triggering the spammed error. Oftentimes the root cause of a spammed error is some other error that only happened once. 
