### Development Gotchas and Common Mistakes

Lists of things that we expect in your PRs, and common mistakes / things to look out for when developing and testing.

#### PR Acceptance
1. Code should be sufficiently commented - all classes, public methods and public fields should have comments. So should any logic that isn't obvious why it's doing something.
2. Code should be indented with tabs and not spaces.
3. Code should compile without new errors or warnings.
4. PR should be tested in editor (most PRs should be tested hosting in editor and joining in a standalone build).
5. Any new files should be named using PascalCase
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
3. Your component / item doesn't work correctly when it is reused via the object pool. Test it by right clicking and choosing the "Respawn" admin option to see how it behaves when it's despawned and respawned (it should behave as if it is a newly spawned instance of its type and have nothing leftover from its previous state).
4. You didn't use syncvars as per our recommendations. See [SyncVar Best Practices](SyncVar-Best-Practices-for-Easy-Networking.md)
