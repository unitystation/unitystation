Firebase Unity SDK
==================

The Firebase Unity SDK provides Unity packages for the following Firebase
features on *iOS* and *Android*:

| Feature                            | Unity Package                     |
|:----------------------------------:|:---------------------------------:|
| Firebase Analytics                 | FirebaseAnalytics.unitypackage    |
| Firebase Authentication            | FirebaseAuth.unitypackage         |
| Firebase Crashlytics               | FirebaseCrashlytics.unitypackage  |
| Firebase Realtime Database         | FirebaseDatabase.unitypackage     |
| Firebase Dynamic Links             | FirebaseDynamicLinks.unitypackage |
| Firebase Functions                 | FirebaseFunctions.unitypackage    |
| Firebase Instance ID               | FirebaseInstanceId.unitypackage   |
| Firebase Messaging                 | FirebaseMessaging.unitypackage    |
| Firebase Realtime Database         | FirebaseDatabase.unitypackage     |
| Firebase Remote Config             | FirebaseRemoteConfig.unitypackage |
| Firebase Storage                   | FirebaseStorage.unitypackage      |

The SDK provides .NET 3.x and .NET 4.x compatible packages in the `dotnet3` and
`dotnet4` directories of the SDK:

* Unity 5.x and earlier use the .NET 3.x framework, so you need to import
  packages from the `dotnet3` directory.
* Unity 2017.x and newer allow the use of the .NET 4.x framework.  If your
  project is configured to use .NET 4.x, import packages from the
  `dotnet4` directory.

## Desktop Workflow Implementations

The Firebase Unity SDK includes desktop workflow support for the following
Firebase features, enabling their use in the Unity editor and in standalone
desktop builds on Windows, OS X, and Linux:

| Feature                            | Unity Package                     |
|:----------------------------------:|:---------------------------------:|
| Firebase Authentication            | FirebaseAuth.unitypackage         |
| Firebase Realtime Database*        | FirebaseDatabase.unitypackage     |
| Firebase Functions                 | FirebaseFunctions.unitypackage    |
| Firebase Remote Config             | FirebaseRemoteConfig.unitypackage |
| Firebase Storage                   | FirebaseStorage.unitypackage      |

(* See Known Issues in the Release Notes below.)

This is a Beta feature, and is intended for workflow use only during the
development of your app, not for publicly shipping code.

## Stub Implementations

Stub (non-functional) implementations of the remaining libraries are provided
for convenience when building for Windows, OS X, and Linux, so that you don't
need to conditionally compile code when also targeting the desktop.

## AdMob

The AdMob Unity plugin is distributed separately and is available from the
[AdMob Get Started](https://firebase.google.com/docs/admob/unity/start) guide.

## Known Issues

### .NET 4.x compatibility when using Unity 2017.x and newer.

.NET 4.x support is available as a build option in Unity 2017 and newer.
Firebase plugins use components of the
[Parse SDK](https://github.com/parse-community/Parse-SDK-dotNET) to provide some
.NET 4.x classes in earlier versions of .NET. Therefore, versions `5.4.0` and
newer of the {{unity_sdk}} provide plugins that are compatible with either
.NET 3.x or .NET 4.x in `dotnet3` and `dotnet4` directories of the
{{unity_sdk_link}}.

If you import a Firebase plugin that is incompatible with the .NET version
enabled in your project, you'll see compile errors from some types in the
.NET framework that are implemented by the Parse SDK.

To resolve the compilation error, if you're using .NET 3.x:

1. Remove or disable the following DLLs for all platforms:
    - `Parse/Plugins/dotNet45/Unity.Compat.dll`
    - `Parse/Plugins/dotNet45/Unity.Tasks.dll`
1. Enable the following DLLs for all platforms:
    - `Parse/Plugins/Unity.Compat.dll`
    - `Parse/Plugins/Unity.Tasks.dll`

To resolve the compilation error, if you're using .NET 4.x:

1. Remove or disable the following DLLs for all platforms:
    - `Parse/Plugins/Unity.Compat.dll`
    - `Parse/Plugins/Unity.Tasks.dll`
1. Enable the following DLLs for all platforms:
    - `Parse/Plugins/dotNet45/Unity.Compat.dll`
    - `Parse/Plugins/dotNet45/Unity.Tasks.dll`

If you import another Firebase plugin:

- Select the menu item
  `Assets > Play Services Resolver > Version Handler > Update`
  to enable the correct DLLs for your project.

### Unity 2017.2 Networking

Realtime Database creates TLS network connections using the .NET networking
stack.  TLS functionality is broken in Unity 2017.2 when using .NET 4.6 causing
the Realtime Database plugin to fail in editor and on desktop.  There is no
workaround for this issue, you have to use a different version of Unity, for
example 2017.1 or 2017.3.


### Unity 4 workarounds

Firebase plugins are not officially supported in Unity 4.  However, we do
make an effort to ensure the plugins can work with some manual setup.

A couple of components do not work in Unity 4:

  - [Version Handler](https://github.com/googlesamples/unity-jar-resolver#unity-plugin-version-management)
    does not work due to no PluginImporter and no clean way to prevent managed
    DLLs from being loaded by Unity 4.
    - This means it's not possible for plugins to automatically enable the most
      recent version of shared components (e.g Firebase, AdMob, Facebook etc.
      may share a common component).
    - DLLs that target a specific .NET version (e.g .NET 4.x) are not disabled.
  - Managed (C#) DLLs cannot be targeted to a specific platform which breaks
    our plugin where we have platform specific C# DLLs for some components.

To use in Unity 4 you will need to:

  - Resolve any dependencies that are shared between multiple plugins.
    For example, Firebase and AdMob use the
    [Play Services Resolver](https://github.com/googlesamples/unity-jar-resolver)
    which contains DLLs that encode the version in their filename under the
    folder `PlayServicesResolver/Editor`.  For each versioned DLL under the
    folder `PlayServicesResolver/Editor` delete the oldest version of each DLL.
  - Remove .NET 4.x DLLs from `Parse/Plugins/dotNet45`.
  - Remove / rename platform specific DLLs.
    Firebase plugins contain iOS specific DLLs under the folder
    `Firebase/Plugins/iOS`.
    - When *not* building for iOS:
      - Change the extension of files under the `Firebase/Plugins/iOS` folder
        from `.dll` to `.dlldisabled`
    - When building for iOS:
      - Change the extension of files under the `Firebase/Plugins/iOS` folder
        from `.dlldisabled` to `.dll`
      - For each file in `Firebase/Plugins/iOS` change the file extension of
        the same name under `Firebase/Plugins` from `.dll` to `.dlldisabled`.
        For example, `Firebase/Plugins/iOS/Firebase.App.dll` and
        `Firebase/Plugins/Firebase.App.dlldisabled`.

Setup
-----

You need to follow the
[SDK setup instructions](https://firebase.google.com/docs/unity/setup).
Each Firebase package requires configuration in the
[Firebase Console](https://firebase.google.com/console).  If you fail to
configure your project your app's initialization will fail.

Support
-------

[Firebase Support](http://firebase.google.com/support/)

Release Notes
-------------
### 6.5.0
  - Overview
    - Updated dependencies, improved logging for Auth and Database, and fixed
      the freeze in the editor.
  - Changes
    - General: The instance of FirebaseApp, FirebaseAuth, FirebaseDatabase,
      FirebaseFunctions, FirebaseInstanceId and FirebaseStorage will be kept
      alive after creation until explicitly disposed.
    - Auth (Linux): Improved error logging if libsecret (required for login
      persistence) is not installed on Linux.
    - Database: The database now supports setting the log level independently of
      the system level logger.
    - Auth/Database (Desktop): Fixed the freeze when playing in the editor for
      the more than once or when closing the editor, when keeping a static
      reference to either FirebaseAuth or FirebaseDatabase instances.

### 6.4.0
  - Overview
    - Updated dependencies, improved error handling in the iOS build logic,
      improved error handling with deleted objects, fixed an issue with Auth
      persistence, and fixed a crash in Database.
  - Changes
    - General: Added more underlying null checks when accessing objects that can
      potentially be deleted, throwing exceptions instead of crashing.
    - General (iOS): Handle malformed Info.plist files when patching Xcode
      projects.
    - Auth (Desktop): Fixed an issue with updated user info not being persisted.
    - Database (Desktop): Fixed a crash with saving a ServerTimestamp during
      a transaction.

### 6.3.0
  - Overview
    - Auth (iOS): Fixed an exception in Firebase.AuthVerifyPhoneNumber.
  - Changes
    - General (Editor): Fixed spurious errors about missing google-services.json
      file.
    - General (iOS/Android): Fixed a bug that allows custom FirebaseApp
      instances to be created after the app has been restarted
    - Auth (Desktop): Changed destruction behavior. Instead of waiting for all
      async operations to finish, now Auth will cancel all async operations and
      quit. For callbacks that are already running, this will protect against
      cases where auth instances might not exist anymore.
    - Auth (iOS): Fixed an exception in PhoneAuthProvider.verifyPhoneNumber.
    - Auth (iOS): Stopped Auth from hanging on destruction if any local tasks
      remain in scope.
    - Database (Desktop): Fixed an issue that could cause a crash when updating
      the descendant of a location with a listener attached.

### 6.2.2
  - Overview
    - Bug fixes.
  - Changes
    - General (Editor): Worked around regression in Unity 2019.2 and 2019.3
      which caused DllNotFoundException.
    - General (Editor, macOS): Add support for macOS 10.11.x.
    - Auth (Editor): After loading a persisted user data, ensure token is
      not expired.
    - Auth (desktop): Ensure Database, Storage and Functions do not use an
      expired token after it's loaded from persistent storage.
    - Database (Editor): Fixed a crash when calling UpdateChildrenAsync.
    - Database (Editor): Deprecated service account authentication.
    - Database (Editor): Fixed DatabaseReference.RunTransaction() sending
      invalid data to the server which causes error message "Error on
      incoming message" and freeze.
  - Known Issues
    - Database/Storage/Functions may fail to send authentication token to server
      if FirebaseAuth is garbage-collected. If you are unable to access to
      the server due to "Permission Denied", please try to keep FirebaseAuth
      alive.

### 6.2.1
  - Overview
    - Fixed Crashlytics on Android not working correctly.
  - Changes
    - Crashlytics (Android): Fixed an issue causing Crashlytics to believe it
      was shut down, blocking all functionality.

### 6.2.0
  - Overview
    - Moved Realtime Database to a C++ implementation on desktop, added support
      for custom domains to Dynamic Links, and fixed issues in Database,
      Instance ID, and Crashlytics.
  - Changes
    - General (Editor): Fixed an issue that could cause errors when trying to
      read a google-services.json file with unicode characters in its path.
    - General (Editor, iOS): Added support for patching Xcode projects in
      Unity 2019.3+.
    - General: Fixed a race that could lead to a crash when gabarge collecting
      FirebaseApp objects.
    - General: Updated Play Services Resolver from 1.2.116 to 1.2.121
      For more information, see [this document](https://github.com/googlesamples/unity-jar-resolver/blob/master/CHANGELOG.md#version-12121---jun-27-2019).
      Added support for the [Jetpack Jetifier](https://developer.android.com/studio/command-line/jetifier)
      , this allows the use of legacy Android support libraries with the latest
      release of Google Play Services that uses AndroidX.
    - Crashlytics (Android): Fixed a crash when logging large call stacks.
    - Crashlytics (Android): Fixed a crash in exception logging when the
      application is shutting down.
    - Instance ID (Android): Fixed a crash when destroying InstanceID objects.
    - Instance ID: Fixed a crash if multiple Instance ID objects are created and
      destroyed quickly.
    - Dynamic Links: Added support for custom domains.
    - Database (Editor): Moved Realtime Database to a C++ implementation on
      desktop to improve reliability across different Unity versions.
    - Database (Editor): Moved transaction callbacks to the main thread to
      mirror Android and iOS.
    - Database: Added a way to configure log verbosity of Realtime Database
      instances.

### 6.1.1
  - Overview
    - Fixed an issue when generating Firebase config files on Windows.
  - Changes
    - General (Editor): Fixed an issue when generating Firebase config files on
      Windows.
    - General (Editor): Upgraded Play Services Resolver to from 1.2.115 to
      1.2.116. For more information see [this
      document](https://github.com/googlesamples/unity-jar-resolver/blob/master/CHANGELOG.md#version-12115---jun-7-2019).

### 6.1.0
  - Overview
    - Added Auth credential persistence on Desktop, fixed and cleaned up some
      documentation, converted testapps to use ContinueOnMainThread(), fixed
      issues in Auth and Database, and added additional information to
      Messaging notifications.
  - Changes
    - General (Editor): Removed Firebase Invites documentation from the
      in-editor documentation.
    - General (Editor): Fixed an issue with resource generation when Firebase
      plugin files have been moved from their default locations.
    - General (iOS): Fixed an issue where connections via NSURLSession
      (used internally by the iOS SDK) can be prematurely closed by the client
      if NSAppTransportSecurity is set to YES in the Info.plist and
      NSAllowsArbitraryLoadsInWebContent is not set. This can be fixed by
      setting NSAllowsArbitraryLoadsInWebContent  to the same value as
      NSAppTransportSecurity.
    - General (Editor): Upgraded Play Services Resolver to from 1.2.109 to
      1.2.115. For more information see [this
      document](https://github.com/googlesamples/unity-jar-resolver/blob/master/CHANGELOG.md#version-12115---may-28-2019).
    - Auth (Desktop): User's credentials will now persist between sessions.  See
      the [documentation](http://firebase.google.com/docs/auth/unity/manage-users#persist_a_users_credential)
      for more information.
    - Auth (Desktop): As part of the above change, if you access CurrentUser
      immediately after creating the FirebaseAuth instance, it will block until
      the saved user's state is finished loading.
    - Auth (Desktop): Fixed an issue where Database/Functions/Storage might not
      use the latest auth token immediately after sign-in.
    - Auth (Android): Fixed an issue where an error code could get reported
      incorrectly on Android.
    - Crashlytics, Functions: Fixed an issue that could cause a crash during
      shutdown due to the destruction order of plugins being nondeterministic.
    - Database (iOS): Fixed a race condition that could cause a crash
      when cleaning up database listeners on iOS.
    - Database (iOS): Fixed an issue where long (64-bit) values could get
      written to the database incorrectly (truncated to 32-bits) on 32-bit
      devices.
    - Messaging (Android): Added channel_id to Messaging notifications.

### 6.0.0
  - Overview
    - Released
      [Crashlytics](https://firebase.google.com/docs/crashlytics/get-started?platform=unity)
      as generally available (GA); added Task.ContinueWithOnMainThread(); fixed
      issues in the Android Resolver, iOS Resolver, Auth, Database, Messaging,
      and Remote Config; removed Firebase Invites, removed deprecated methods in
      Firebase Remote Config, and deprecated a method in Firebase Analytics.
  - Changes
    - Updated [Firebase
      iOS](https://firebase.google.com/support/release-notes/ios#6.0.0) and
      [Firebase
      Android](https://firebase.google.com/support/release-notes/ios#2019-05-07)
      dependencies.
    - Crashlytics (iOS/Android): [Crashlytics for
      Unity](https://firebase.google.com/docs/crashlytics/get-started?platform=unity)
      is now generally available (GA). Get the next evolution with BigQuery
      exports, Jira integration, and more. To migrate from Fabric Crashlytics
      for Unity to Firebase Crashlytics, follow the [migration
      guide](https://firebase.google.com/docs/crashlytics/migrate-from-fabric).
    - Added an extension method, `Task.ContinueWithOnMainThread()`, which
      forces the continuation of asynchronous operations to occur in the Unity
      main thread rather than in a background thread.
    - General: Upgraded Play Services Resolver to from 1.2.104 to 1.2.109. For
      more information see [this
      document](https://github.com/googlesamples/unity-jar-resolver/blob/master/CHANGELOG.md#version-12109---may-6-2019).
    - General (Android): Added support for Android SDK installed directly in
      Unity 2019.
    - General (iOS): Fixed issues generating projects without using Cocoapods.
    - Database (iOS/Android): Fixed an issue where integrating the SDK greatly
      increased the size of your app.
    - Database: Fixed exception handling during listener events.
    - Remote Config: Fixed an issue parsing boolean values.
    - Auth (Desktop): Fixed a crash when attempting to call Game Center
      authentication methods from the Unity editor.
    - Messaging (iOS/Android): Fix an issue where Subscribe and Unsubscribe
      never returned if the API was configured not to receive a registration
      token.
    - Invites: Removed Firebase Invites, as it is no longer supported.
    - Remote Config: Removed functions using config namespaces.
    - Analytics: Deprecated SetMinimumSessionDuration.

### 5.7.0
  - Overview
    - Fixed an issue with escape characters in Auth, deprecated functions
      in Remote Config, and fixed an issue in the Android Resolver.
  - Changes
    - Auth: Fixed UserProfile.PhotoUrl removing percent encoded characters when
      being set.
    - Remote Config: Config namespaces are now deprecated. You'll need to switch
      to methods that use the default namespace.
    - General (Android): Fixed an exception on resolution in some versions of
      Unity 2017.4 by changing how Android ABI selection is handled.

### 5.6.1
  - Overview
    - Fixed race condition on iOS SDK startup and fixed some issues in the
      Android Resolver.
  - Changes
    - General (iOS): Updated to the latest iOS SDK to fix a crash on
      firebase::App creation caused by a race condition.  The crash could occur
      when accessing the [FIRApp firebaseUserAgent] property of the iOS FIRApp.
    - General (Android): Fixed Java version check in Android resolver when using
      Java SE 12 and above.
    - General (Android): Whitelisted Unity 2017.4 and above for ARM64 builds.
      Previously required ARM64 libraries would be stripped from all Unity 2017
      builds resulting in a DllNotFoundException.

### 5.6.0
  - Overview
    - Added Game Center sign-in to Auth and fixed intermittent crashes due to
      garbage collection.
  - Changes
    - Auth (iOS): Added Game Center authentication.
    - General: Fixed intermittent crashes caused when multiple native objects
      were garbage-collected at the same time.

### 5.5.0
  - Overview
    - Added support for
      [Crashlytics](https://firebase.google.com/docs/crashlytics/get-started#unity)
      as a Beta release, deprecated Firebase Invites, and updated the Android
      Resolver.
  - Changes
    - Crashlytics:
      [Crashlytics for Unity](https://firebase.google.com/docs/crashlytics/get-started#unity)
      is now available as a Beta release. Get the next evolution with BigQuery
      exports, Jira integration, and more. To migrate from Fabric Crashlytics
      for Unity to Firebase Crashlytics, follow the
      [migration guide](https://firebase.google.com/docs/crashlytics/migrate-from-fabric).
    - General (Android): Updated to using version 1.2.101 of the Android
      Resolver. Prompt the user before the resolver runs for the
      first time and allow the user to elect to disable from the prompt.
    - Invites: Firebase Invites is deprecated. Please refer to
      https://firebase.google.com/docs/invites for details.

### 5.4.4
  - Overview
    - Fixed bugs in iOS/Android Resolver components, Realtime Database on
      mobile, and Cloud Functions on Android; fixed a general iOS bug; and fixed
      issues with Unity 5.6 and Unity 2018.3 and newer.
  - Changes
    - General (Android): Fixed packaging of AARs in the Android Resolver when
      using Unity 2018 and a recent version of Gradle.
    - General: Reduced auto-resolution frequency in iOS and Android Resolvers,
      speeding up builds and reducing memory footprint.
    - General: Fixed an issue with version number handling in iOS and Android
      Resolvers.
    - General (iOS): Fixed an issue that caused apps to crash when exiting the
      app.
    - General: Fixed parsing of Unity 5.6 metadata.
    - General: Workaround for Unity 2018.3 and newer ignoring the "Any"
      platform.
    - Realtime Database (mobile): Fixed an issue where certain DataSnapshots
      were missing data.
    - Cloud Functions (Android): Fixed an issue with error handling.
  - Known Issues
    - The garbage collection race condition mentioned	in version 5.4.2 still
      occurs in Firebase Auth, Database, Storage, and Instance ID. To work
      around the issue until a fixed is released, keep a reference to the
      Firebase object instance (for example, FirebaseAuth.DefaultInstance) to
      prevent garbage collection.

### 5.4.3
  - Overview
    - Bug fix for Firebase Storage on iOS.
  - Changes
    - Storage (iOS): Fixed an issue when downloading files with `GetBytesAsync`.

### 5.4.2
  - Overview
    - Updated iOS and Android dependency versions, and fixed issues in the
      Android Resolver, FirebaseApp, Auth on Android, Database, and Dynamic
      Links on iOS.
  - Changes
    - General (Android): Fixed an infinite loop in Android Resolver when using
      auto-resolution.
    - App: Fixed a race condition causing an occasional crash when FirebaseApp
      is garbage collected.
    - Auth (Android): Removed an irrelevant error about the Java class
      FirebaseAuthWebException.
    - Database: Fixed a race condition causing an occasional crash when
      FirebaseDatabase is garbage collected.
    - Dynamic Links (iOS): Fixed Dynamic Links iOS when using Unity Cloud
      builds.
  - Known Issues
    - The garbage collection race condition mentioned above still occurs in
      Firebase Auth, Storage, and Instance ID. To work around the issue until a
      fixed is released, keep a reference to the Firebase object instance (for
      example, FirebaseAuth.DefaultInstance) to prevent garbage collection.

### 5.4.1
  - Overview
    - Fix for Google Analytics iOS dependency.
  - Changes
    - Analytics (iOS): Fixed issue with Google Analytics and Google App
      Measurement mismatch.

### 5.4.0
  - Overview
    - Improved support for .NET 4.x Unity projects, exposed method to enable
      Realtime Database peristence, bug fix for link shortening in
      Dynamic Links.
  - Changes
    - General: Added plugins that are pre-configured for import into .NET 4.x
      Unity projects.
    - Realtime Database: Exposed method to enable persistence on mobile
      platforms.
    - Dynamic Links (Android): Fixed short link generation failing with
      "error 8".

### 5.3.1
  - Overview
    - Updated iOS and Android dependency versions, bug fix for Invites,
      improved Android module initialization, fixed issue with Unity 2018.3
      beta, added C# symbols and upgraded the Play Services Resolver.
  - Changes
    - General: Added symbols for all C# assemblies.
    - General (Android): Improved module initialization so that the Unity SDK
      does not attempt to use Android libraries unless the C# assembly is
      included.  For example, this allows users of the Firebase Analytics plugin
      to use the `firebase-messaging` Android library without the Firebase Unity
      Messaging component.
    - General (Editor): Fixed loading of the Firebase.Editor.dll component in
      Unity 2018.3.0b2
    - General (Editor): Updated the Play Services Resolver from version 1.2.88
      to 1.2.91, see the
      [GitHub changelog](https://github.com/googlesamples/unity-jar-resolver/blob/master/CHANGELOG.md)
      for details.
    - General (Editor): Fixed the Android "Open in Console" button of the
      Firebase window (accessible under the **Window > Firebase** menu option)
      to correctly open the Firebase console in a web browser when the selected
      target platform is not Android in Unity 5.6 and above.
    - Invites (Android): Fixed an exception when the Android Minimum Version
      code option is used on the Android.

### 5.3.0
  - Overview
    - Fixed bugs in Database, Functions, Storage, and the Android Resolver;
      changed minimum Xcode version to 9.4.1.
  - Changes
    - General (iOS): Minimum Xcode version is now 9.4.1.
    - General (Android): Fixed an issue resolving additional types of version
      conflicts in the Android Resolver.
    - General (Android): Fixed a hang in Unity 5.6.
    - Database (Desktop): Fixed issues in ChildListener.
    - Database (Desktop): Fixed a crash related to objects being garbage
    - Functions (Android): Fixed an issue when a function returns an array.
    - Storage: Fixed issues when transactions are canceled in .NET 4.6.
  - Known Issues
    - Dynamic Links (Android): Shortening dynamic links fails with "Error 8".

### 5.2.1
  - Overview
    - Updated Android and iOS dependency versions, and fixed bugs in App, Auth,
      Database, and the Android Resolver.
  - Changes
    - General (Android): Fixed an issue resolving certain types of version
      conflicts in the Android Resolver.
    - App: Now throws an exception if any Firebase libraries are initialized
      while `CheckAndFixDependenciesAsync()` is still in progress.
    - Auth, Database: Fixed a race condition returning Tasks when calling
      the same method twice in quick succession.
    - Database (iOS/Android): Fixed a crash in DatabaseReference/Query during
      garbage collection (and other times).

### 5.2.0
  - Overview
    - Fixed bugs in Auth, changes to Functions, Messaging and Android builds.
  - Changes
    - Auth: Fixed per-frame allocation in the token refresh logic.
    - Auth (Android): Fixed a crash in
      `FirebaseUser.UpdatePhoneNumberCredentialAsync()`.
    - Functions: Added a way to specify which region to run the function in.
    - Messaging: Added `SubscribeAsync` and `UnsubscribeAsync`, which return
      Tasks, and deprecated `Subscribe` and `Unsubscribe`.
    - General (Android): Fixed a null reference in the Google Play Services
      availability checker.
    - General (Android): Fixed Android problems merging Android library
      manifests in Unity 2018.
    - General (Android): Added arm64-v8a build support.

### 5.1.1
  - Overview
    - Updated Android and iOS dependency versions only.

### 5.1.0
  - Overview
    - Changes to Analytics, Auth, and Database; and added Cloud Functions for
      Firebase.
  - Changes
    - Android (General): Fixed build issues due to the broken AndroidManifest
      merger in Unity 2018.x.
    - Android (General): Improved compatibility with plugins that use Google
      Play services versions older than 15.0.0.
    - Android (General): Improved dependency resolution when the Android SDK
      path is not configured.
    - Analytics: Added `ResetAnalyticsData()` to clear all analytics data
      for an app from the device.
    - Analytics: Added `GetAnalyticsInstanceIdAsync()` which allows developers
      to retrieve the current app's analytics instance ID.
    - Auth: Linking a credential with a provider that has already been linked
      now produces an error.
    - Auth (iOS): Fixed crashes in
      `FirebaseUser.LinkAndRetrieveDataWithCredential()` and
      `FirebaseUser.ReauthenticateAndRetrieveData()`.
    - Auth (iOS): Fixed photo URL never returning a value on iOS.
    - Auth (Android): Fixed setting the profile photo URL with
      `FirebaseUser.UpdateUserProfile()`.
    - Database: Added support for ServerValues in SetPriority methods.
    - Database (iOS / Android): Now implemented as a wrapper around Firebase iOS
      and Android SDKs, to add offline support and increase reliability and
      performance.
    - Functions: Added support for Cloud Functions for Firebase on iOS, Android,
      and desktop.

### 5.0.0
  - Overview
    - Renamed the static libraries to include firebase in their name,
      removed deprecated methods in App, Auth, and Storage,
      and exposed new APIs in Dynamic Links and Invites.
  - Changes
    - General: Library names that previously did not mention Firebase now have
      a "FirebaseCpp" prefix. For example, Auth.dll is now FirebaseCppAuth.dll.
    - General (Android): Improved error handling when device is out of space.
    - App: Removed deprecated method SetLogLevel.
    - Auth: Removed deprecated properties PhotoUri and RefreshToken.
    - Dynamic Links: Added MatchStrength to ReceivedDynamicLink, that describes
      the strength of the match for the received link.
    - Invites: Added MatchStrength to InvitesReceivedEventArgs, that describes
      the strength of the match for the received invite.
    - Storage: Deprecated StorageMetadata.DownloadUrl and
      StorageMetadata.DownloadUrls.
      Please use StorageReference.GetDownloadUrlAsync() instead.
    - Messaging: Added an optional initialization options struct. This can be
      used to suppress the prompt on iOS that requests permission to receive
      notifications at start up. Permission can be requested manually using the
      function `FirebaseMessaging.RequestPermissionAsync()`.

### 4.5.2
  - Overview
    - Fixed a build issue, and bugs in FirebaseApp, Auth and Linux Desktop.
  - Changes
    - Common: Updated Parse .NET 4.6 forwarding DLLs to fix build issues when
      using IL2CPP with the .NET 4.6 framework.  The update works with IL2CPP in
      Unity 2017.2 and beyond.  IL2CPP builds still fail in Unity 2017.1
      as the IL2CPP distribution bundled with Unity 2017.1 does not correctly
      support type forwarding DLLs.
    - Common: Root cert installation is now *only* performed in plugins that
      use the .NET network stack (currently only the Realtime Database).  This
      should resolve exceptions on initialization that reference
      `/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation`.
    - Common (Desktop): Fixed crash when using Firebase Auth, Storage,
      Realtime Database and Remote Config on Linux.
    - Common (Android): Loading default AppOptions is now more robust on Android
      resulting in errors reported for missing fields rather than exiting the
      application with an error in the native library.
    - Auth: Fixed regression in release 4.5.0 which led to an unhandled
      exception on auth token refresh.
  - Known Issues
    - IL2CPP builds will fail in Unity 2017.1 as the IL2CPP distribution bundled
      with Unity 2017.1 does not correctly support type forwarding DLLs.

### 4.5.1
  - Overview
    - Fixed some build issues, fixed bugs in Database, Dynamic Links,
      Invites, Remote Config, and Storage, and exposed new APIs in Auth on
      Desktop and Analytics.
  - Changes
    - Auth (Desktop): Added support for accessing user metadata.
    - Analytics: Added SetMinimumSessionDuration() and
      SetSessionTimeoutDuration().
    - Desktop: Fixed a bug when using iOS GoogleServicesInfo.plist config
      settings on desktop, which prevented "play in editor" mode from loading
      the correct project settings. (Only affected users who developed for iOS
      and not Android, who were using Desktop).
    - Dynamic Links and Invites (Android): Fixed an issue with Dynamic Links
      getting lost when calling CheckAndFixDependenciesAsync.
    - Messaging: Added TokenRegistrationOnInitEnabled property to enable or
      disable auto-token generation.
    - Remote Config: Fixed a bug causing incorrect reporting of success or
      failure during a Fetch().
    - Storage: Fixed a bug in Storage that was unescaping '/' characters in
      URL returned by StorageReference.GetDownloadUrlAsync(). This caused an
      "Invalid HTTP method/URL pair" error when attempting to download using the
      URL.
    - General (Android): Fixed a bug causing Unity to hang due to FirebaseApp
      initializing on the wrong thread, when building in -batchmode.

### 4.5.0
  - Overview
    - Desktop workflow support for some features, Google Play Games
      authentication on Android, improved editor support, and changes to Auth,
      Instance ID and Storage.
  - Changes
    - General: Added support for plugins within the Unity Editor context.
    - Auth, Remote Config: Stub implementations have been replaced with
      functional desktop implementations on Windows, OS X and Linux.
    - Auth (Android): Added support for Google Play Games authentication.
    - Auth, Instance ID: Fixed issues when destroying/finalizing Firebase
      objects.
    - Storage: Added Md5Hash to StorageMetadata.
    - Storage (iOS / Android): Now implemented as wrapper around Firebase iOS
      and Android SDKs, to increase reliability and performance.
  - Known Issues
    - On Windows and Mac OS, only 64-bit builds are supported (x86_64), not
      32-bit.

### 4.4.3
  - Overview
    - Bug fixes in Dynamic Links, Invites, Remote Config and Storage.
  - Changes
    - Dynamic Links (iOS): Now fetches the invite ID when using universal links.
    - Dynamic Links (iOS): Fixed crash on failure of dynamic link completion.
    - Dynamic Links (iOS): Fixed an issue where some errors weren't correctly
      reported.
    - Invites (Editor): Fixed SendInvite never completing.
    - Remote Config (iOS): Fixed an issue where some errors weren't correctly
      reported.
    - Storage: Fixed Metadata::content_language returning the wrong data.
    - Storage (iOS): Reference paths formats are now consistent with other
      platforms.
    - Storage (iOS): Fixed an issue where trying to upload to a non-existent
      path would not complete the Task.
    - Storage (iOS): Fixed a crash when a download fails.
    - Editor: Fixed a crash in the editor when using .NET 4.6 with certain
      versions of Unity 2017.
    - General (Android): Fixed an issue when Google Play Services was out of
      date and would hang after returning from the update workflow.

### 4.4.2
  - Overview
    - Updated Firebase iOS dependency version.
  - Changes
    - General (iOS): Updated Firebase iOS Cocoapod dependency version.

### 4.4.1
  - Overview
    - Bug fixes for .Net 4.x, Storage, Realtime Database, and Instance ID on
      iOS.
  - Changes
    - Instance ID (iOS): GetTokenAsync no longer fails without an APNS
      certificate, and no longer forces registering for notifications.
    - Storage: Added support for a progress listener and cancellation
      token to `GetBytesAsync`.
    - Storage: Fixed an issue where the auth token was not refreshed when the
      application is started.
    - Realtime Database: Fixed an issue where the auth token was not refreshed
      when the application is started.
    - General (Android): Fixed a bug with handling transitive dependencies in
      the Android Resolver, where there was a common dependency name from
      different sources.
    - General (Android): Fixed Android Resolver reporting non-existent
      conflicts.
    - General: Fixed 'get_realtimeSinceStartup' Assert in development builds.
    - General: Fixed issues when using types added in .NET 4.x such as Tuple.
      This requires switching to the appropriate Unity.Compat.dll when using
      .NET 4.x (see Known Issues).

### 4.4.0
  - Overview
    - Support for Instance ID, and an Auth fix.
  - Changes
    - Instance ID: Added Instance ID library.
    - Auth: Fixed user metadata property names.

### 4.3.0
  - Overview
    - General threading / callback and other bug fixes and new features in Auth.
  - Changes
    - General: Fixed some invalid calls to Unity APIs from threads.
    - General (Editor): Changed Firebase settings window to work with Unity 4.x
    - General (Editor): Fixed GoogleServices-Info.plist not being read in batch
      mode.
    - Auth: Fixed a bug due to a race condition fetching the authentication
      token which could cause Database and Storage operations to hang.
    - Auth: Added support for accessing user metadata.
    - Remote Config (Android): Fixed a bug where remote config values retrieved
      were misclassified as coming from a default config vs an active config.
    - Database: Fixed hang when Time.timeScale is 0.
    - Storage: Fixed hang when Time.timeScale is 0.

### 4.2.1
  - Overview
    - Bug fixes for Real-Time Database, Storage, API initialization in .NET 4.x,
      and improvements to the iOS and Android Resolver components.
  - Changes
    - General (Android): Fixed Android resolution when a project path contains
      apostrophes.
    - General (iOS): Increased speed of iOS resolver dependency loading.
    - General (Android): Removed legacy resolution method from Android Resolver.
      It is now only possible to use the Gradle or Gradle prebuild resolution
      methods.
    - General (Android): Fixed Android Resolution issues with OpenJDK by
      updating the Gradle wrapper to 4.2.1.
    - General (Android): Android resolution now also uses
      gradle.properties to pass parameters to Gradle in an attempt to workaround
      problems with command line argument parsing on Windows 10.
    - General: Fixed some invalid calls to Unity APIs from threads, when using
      .NET 4.x which is added in Unity 2017.
    - Database: Fixed hang in Real-Time Database when Time.timeScale is 0 in
      Unity 2017.
    - Storage: Fixed hang in Storage when Time.timeScale is 0 in Unity 2017.
    - Storage: Fixed file download in Unity 2017.2.

### 4.2.0
  - Overview
    - Added URL support in Messaging, improved the initialization process on
      Android and fixed bugs in the iOS and Android build systems, Analytics,
      Auth, Database and Messaging.
  - Changes
    - Messaging: Messages sent to users can now contain a link URL.
    - Auth: Added more specific error codes for failed operations.
    - Auth (iOS): Phone Authentication no longer requires push notifications.
      When push notifications aren't available, reCAPTCHA verification is used
      instead.
    - Analytics (iOS): Fixed bug which prevented the user ID and user
      properties being cleared.
    - Database: Fixed issue where user authentication tokens are ignored if
      the application uses the database API before initializing authentication.
    - Messaging (Android): Fixed a bug which prevented the message ID field
      being set.
    - General (iOS): Fixed incorrect processing of framework modulemap files
      which resulted in the wrong link flags being generated when Cocoapod
      project integration is enabled.
    - General (Android): Added support for Google Play services dependency
      resolution when including multiple plugins (e.g AdMob, Google Play Games
      services) that require different versions of Google Play services.
    - General (Android): Fixed Android dependency resolution when local
      project paths contain spaces.
    - General (Android): Fixed race condition in Android Resolver which could
      cause a hang when running auto-resolution.
    - General (Android): Forced Android Gradle resolution process to not use
      the Gradle daemon to improve reliability of the process.
    - General (Android): Added a check for at least JDK 8 when running Android
      dependency resolution.
    - General: Fixed MonoPInvokeCallbackAttribute incorrectly being added to
      the root namespace causing incompatibility with plugins like slua.
  - Known Issues
    - General (Android): Unity (not the Firebase SDK) has a bug that causes
      applications to crash after running the Google Play services update on
      Android 8.0 Oreo devices.

### 4.1.0
  - Overview
    - Bug fixes for the iOS build system, Auth, Messaging, and Remote Config.
  - Changes
    - General (iOS): Fixed spurious errors on initialization of FirebaseApp.
    - General (iOS): Fixed iOS build with Cocoapod Project integration enabled.
      This affected all iOS builds when using Unity 5.5 or below or when using
      Unity Cloud Build.
    - General (iOS): Fixed issue which prevented the use of Unity Cloud Build
      with Unity 5.6 and above.  Unity Cloud Build does not open generated
      Xcode workspaces so we force Cocoapod Project integration in the
      Unity Cloud Build environment.
    - Auth (Android): Now throws an exception if you call GetCredential without
      an Auth instance created.
    - Messaging (Android): Fixed a bug resulting in FirebaseMessages not having
      their MessageType field populated.
    - Messaging (iOS): Fixed a race condition if a message is received before
      Firebase Cloud Messaging is initialized.
    - Messaging (iOS): Fixed a bug detecting whether the notification was opened
      if the app was running in the background.
    - Remote Config: When listing keys, the list now includes keys with defaults
      set, even if they were not present in the fetched config.

### 4.0.3
  - Overview
    - Bug fixes for Database, Dynamic Links, Messaging, iOS SDK compatibility,
      .NET 4.x compatibility.
  - Changes
    - General: Added support for .NET 4.x in the System.Task implementation
      used by the SDK.  The VersionHandler editor plugin is now used to switch
      Task implementations based upon the selected .NET version.
    - General: Fixed root cert installation failure if Firebase is initialized
      after other network operations are performed by an application.
    - General: Improved native shared library name mangling when targeting
      Linux.
    - General (iOS): Fixed an issue which resulted in custom options not being
      applied to FirebaseApp instances.
    - General (iOS): Fixed a bug which caused method implementation look ups
      to fail when other iOS SDKs rename the selectors of swizzled methods.
      This could result in a hang on startup when using some iOS SDKs.
    - Dynamic Links (Android): Fixed task completion if short link
      creation fails.
    - Database: Fixed a bug that caused database connections to fail when
      using the .NET 4.x framework in Unity 2017 on OSX.
    - Database: Fixed a bug where large data updates could be ignored.
    - Messaging (iOS): Fixed message handling when messages they are received
      via the direct channel to the FCM backend (i.e not via APNS).

### 4.0.2
  - Overview
    - Bug fixes for Analytics, Auth, Dynamic Links, and Messaging;
      added support for Android SDK 25.
  - Changes
    - General (Android): Fixed a manifest issue with Android SDK tools and
      support library >= 25.x.
    - General (Android): Fixed an issue which caused Analytics to not be
      enabled in all plugins.
    - General (Android): Fixed native libraries not being included in built
      APKs when using the internal build system in Unity 2017.
    - Analytics (Android): Fix SetCurrentScreen to work from any thread.
    - Auth (iOS): Fixed user being invalidated when linking a credential fails.
    - Dynamic Links: Fixed an issue which caused an app to crash or not receive
      a Dynamic Link if the link is opened when the app is installed and not
      running.
    - Messaging (iOS): Fixed a crash when no notification event is registered.
    - Messaging: Fixed token notification event occasionally being raised twice
      with the same token.

## 4.0.1
  - Overview:
    - Bug fixes for Dynamic links and Invites on iOS, the Google Play
      services updater when using Cloud Messaging and Cloud Messaging on iOS.
  - Changes:
    - Cloud Messaging (Android): Fixed crash when updating Google Play services
      in projects that include the Cloud Messaging functionality.
    - Cloud Messaging (iOS): Fixed an issue where library would crash on start
      up if there was no registration token.
    - Dynamic Links & Invites (iOS): Fixed an issue that resulted in apps not
      receiving a link when opening a link if the app is installed and not
      running.

## 4.0.0
  - Overview
    - Added support for phone number authentication, access to user metadata,
      a standalone dynamic links plugin and bug fixes.
  - Changes
    - Auth: Added support for phone number authentication.
    - Auth: Added the ability to retrieve user metadata.
    - Auth: Moved token notification into a separate token change event.
    - Dynamic Links: Added a standalone Unity plugin separate from Invites.
    - Invites (iOS): Fixed an issue in the analytics SDK's method swizzling
      which resulted in dynamic links / invites not being sent to the
      application.
    - Messaging (Android): Fixed a regression introduced in 3.0.3 which caused
      a crash when opening up a notification when the app is running in the
      background.
    - Messaging (iOS): Fixed interoperation with other users of local
      notifications.
    - General (Android): Fixed crash in some circumstances after resolving
      dependencies by updating Google Play services.
    - General (Editor): Fixed iOS resolver and Jar resolver plugins getting
      disabled when importing multiple Firebase, Google Play Games or AdMob
      plugins into a project.
    - General (iOS): Added support for Cocoapod builds that use Xcode
      workspaces in Unity 5.6 and above.
    - General (iOS): Fixed Cocoapod version pinning which was broken in 3.0.3
      causing the SDK to pull in the most recent Firebase iOS SDK rather than
      the correct version for the current Unity SDK release.

## 3.0.3
  - Overview
    - Bug fixes for Auth.
  - Changes
    - Auth: Fixed a crash caused by a stale memory reference when a
      firebase::auth::Auth object is destroyed and then recreated for the same
      App object.
    - Auth: Fixed potential memory corruption when AuthStateListener is
      destroyed.
    - Auth: Fixed occasional crash in Unity editor when using Auth sign-in
      methods.
## 3.0.2
  - Overview
    - Bug fixes for Auth, Database, Invites, Messaging, Storage, and a general
      fix, plus improved compatibility with Unity 5.6 when using the GoogleVR
      SDK.
  - Changes
    - General (Android): Fixed unhandled exception if FirebaseApp creation
      fails due to an out of date Google Play services.
    - General (Android): Fixed Google Play Services updater crash when clicking
      outside of the dialog on Android 4.x devices.
    - Auth: Fixed user being invalidated when linking a credential fails.
    - Auth: Fixed an occasional crash when events are fired.  This could
      manifest in a crash when signing in.
    - Auth: Deprecated FirebaseUser.RefreshToken.
    - Database: Fixed an issue which caused the application to manually
      refresh the auth token.
    - Messaging: Fixed incorrectly notifying the app of a message when a
      notification is received while the app is in the background and the app
      is then opened by via the app icon rather than the notification.
    - Invites (iOS): Fixed an issue which resulted in the app delegate method
      application:openURL:sourceApplication:annotation: not being called
      when linking the invites library.  This caused the Facebook SDK login
      flow to fail.
    - Storage: Fixed a bug that prevented the construction of Metadata without
      a storage reference.
    - Editor (Android): Fixed referenced Android dependencies in maven
      where the POM references a specific version e.g '[1.2.3]'.
    - Editor (iOS): Improved compatibility with Unity 5.6's Cocoapods support
      required to use the GoogleVR SDK.
    - Editor (Android): Fixed Android dependency resolution when the bundle ID
      is modified.

## 3.0.1
  - Overview
    - Fixed Google Play Services checker on Android and improved Android
      build configuration checks.
  - Changes
    - (Android): Fixed Google Play Services checker on Android.  Previously
      when Google Play Services was out of date,
      FirebaseApp.CheckDependencies() incorrectly returned
      DependencyStatus.Available.
    - Editor (Android): Added check for auto-resolution being enabled in the
      Android Resolver.
      If auto-resolution is disabled by the user or by another plugin
      (e.g Google Play Games), the user is warned about the configuration
      problem and given the opportunity to fix it.
    - (Android) Fixed single architecture builds when using Gradle.
    - (Android) Resolved an issue which caused the READ_PHONE_STATE
      permission to be requested.

## 3.0.0
  - Overview
    - Streamlined editor integration, build support and some bug fixes for
      Auth, Database, Messaging, Invites and Storage.
  - Changes
    - Added link.xml files to allow byte stripping to be enabled.
    - Fixed issues with Android builds when targeting a single ABI.
    - Auth: Fixed race condition when accessing user properties.
    - Auth: Added SetCurrentScreen() method.
    - Database: Resolved issue where large queries resulted in empty results.
    - Database: Fixed an issue which prevented saving boolean values.
    - Mesaging: Fixed issue with initialization on iOS that caused problems
      with other SDKs.
    - Invites: Fixed issue with initialization on iOS that caused problems
      with other SDKs.
    - Storage: Fixed a bug which prevented download URLs from containing
      slashes.
    - Storage: Fixed a bug on iOS which caused networking to fail when the
      full .NET 2.0 is used.
    - Editor: Added process of cleaning stale / moved files when upgrading
      to a newer plugin version.
    - Editor: Automated Cocoapod tool installation and improved Pod tool
      detection when using RVM.  This enables iOS projects to build with
      Unity Cloud Build.
    - Editor: Added support for pods that reference static libraries.
    - Editor: Bundle ID selection dialog for iOS and Android is now displayed
      when the project bundle ID doesn't match the Firebase configuration.
    - Editor: Added experimental support for building with Proguard stripping
      enabled.
    - Editor: Fixed Android package (AAR) synchronization when the project
      bundle ID is modified.
    - Editor: Fixed clean up of stale AAR dependencies when users change
      Android SDK versions.
    - Editor: Android Jar Resolver now remembers - for the editor session -
      which AARs to keep when new AARs are available compared to what is
      included in a project.
    - Editor: Added support for projects that use Google Play Services at
      different versions.
    - Editor: Fixed minor issue with the Firebase window not being repainted as
      Firebase configuration files are added to or removed from a project.
    - Desktop: Added fake - but valid - JWT in the Authentication mock.


## 1.1.2
  - Overview
    - Fix for a major bug causing Auth to hang, as well as other bug fixes.
  - Changes
    - Auth: Fixed a potential deadlock when running callbacks registered via
      Task.ContinueWith()
    - Auth: (Android) Fixed an error in `Firebase.Auth.FirebaseUser.PhotoUrl`.
    - Messaging: (iOS) Removed hard dependency on Xcode 8.
    - Messaging: (Android) Fixed an issue where the application would receive an
      empty message on startup.

## 1.1.1
  - Overview
    - Bug fixes for the editor plugin, Firebase Authentication, Messaging,
      Invites, Real-Time Database and Storage.
  - Changes
    - Fixed an issue in the editor plugin that caused an exception to be
      thrown when the project bundle ID didn't match a bundle ID in the Android
      configuration file (google-services.json).
    - Fixed a bug in the editor plugin that caused a stack overflow when
      multiple iOS configuration files (GoogleServices-Info.plist) are
      present in a project.
    - Auth: (Android) Fixed an issue that caused a Task to never complete
      when signing in while a user is already signed in.
    - Auth: Renamed the Auth.UserProfile.ProtoUri property to
      Auth.UserProfile.ProtoUrl in order to be consistent with the other URL
      properties across the SDK.
    - Messaging / Invites: Fixed an issue with method swizzling that caused
      some of the application's UIApplicationDelegate methods to not be called.
    - Storage: The Storage  plugin was using a Unity API that is only
      present in Unity 5.4. We have modified the component so that it is now
      backwards compatible with previous versions of Unity.
    - Real-Time Database: Fixed an issue that prevented saving floating point
      values.

## 1.1.0
  - Overview
    - Added support for Firebase Storage and bug fixes.
  - Changes
    - Added support for Firebase Storage.
    - Fixed crash in Firebase Analytics when logging arrays of parameters.
    - Fixed crash in Firebase Messaging when receiving messages with empty
      payloads on Android.
    - Fixed random hang when initializing Firebase Messaging on iOS.
    - Fixed topic subscriptions in Firebase Messaging.
    - Fixed an issue that resulted in a missing app icon when using Firebase
      Messaging on Android.
    - Fixed exception in error message construction when FirebaseApp
      initialization fails.
    - Fixed reporting of null events in the Firebase Realtime Database.
    - Fixed unsubscribe for complex queries in the Firebase Realtime Database.
    - Fixed service account authentication in the Firebase Realtime Database.
    - Fixed Firebase.Database.Unity being stripped from iOS builds.
    - Fixed support for building with Firebase plugins in Microsoft
      Visual Studio.
    - Fixed scene transitions causing event routing to break across all
      components.
    - Changed editor plugins for Firebase Authentication and Invites to
      return success for all operations instead of raising exceptions.
    - Changed editor plugin to read JAVA_HOME from the Unity editor
      preferences.
    - Changed editor plugin to scan all google-services.json and
      GoogleService-Info.plist files in the project and select the config file
      matching the project's current bundle ID.
    - Improved the performance of AAR / JAR resolution when the Android config
      is selected and auto-resolution is enabled.
    - Improved error messages in the editor plugin.
  - Known Issues
    - Proguard is not integrated into Android builds. We have distributed
      proguard files that can be manually integrated into Android builds
      within AAR files matching the following pattern in each
      Unity package:
      `Firebase/m2repository/com/google/firebase/firebase-*-unity/*firebase-*.srcaar`
    - Incompatible AARs are not resolved correctly when building for Android.
      This can require manual intervention when using multiple plugins
      (e.g Firebase + AdMob + Google Play Games).  A workaround is documented
      on the
      [AdMob Unity plugin issue tracker](https://github.com/googleads/googleads-mobile-unity/issues/314).

## 1.0.1
  - Overview
    - Bug fixes.
  - Changes
    - Fixed Realtime Database restricted access from the Unity Editor on
      Windows.
    - Fixed load and build errors when iOS support is not installed.
    - Fixed an issue that prevented the creation of multiple FirebaseApp
      instances and customization of the default instance on iOS.
    - Removed all dependencies on Python for Android resource generation on
      Windows.
    - Fixed an issue with pod tool discovery when the Ruby Gem binary directory
      is modified from the default location.
    - Fixed problems when building for Android with the IL2CPP scripting
      backend.
  - Known Issues
    - Proguard is not integrated into Android builds. We have distributed
      proguard files that can be manually integrated into Android builds
      within AAR files matching the following pattern in each
      Unity package:
      `Firebase/m2repository/com/google/firebase/firebase-*-unity/*firebase-*.srcaar`

## 1.0.0
  - Overview
    - First public release with support for Firebase Analytics,
      Authentication, Real-time Database, Invites, Dynamic Links and
      Remote Config.
      See our
      [setup guide](https://firebase.google.com/docs/unity/setup) to
      get started.
  - Known Issues
    - Proguard is not integrated into Android builds.  We have distributed
      proguard files that can be manually integrated into Android builds
      within AAR files matching the following pattern in each
      Unity package:
      `Firebase/m2repository/com/google/firebase/firebase-*-unity/*firebase-*.srcaar`
