# Azure Spatial Anchors Plugin for Unity

## Building in General
1. Navigate to `AzureSpatialAnchors.SDK/Resources`
2. Select **SpatialAnchorConfig**
3. Set `Spatial Anchors Account Id` to the value provided by the Azure portal. 
4. Set `Spatial Anchors Account Key` to the value provided by the Azure portal. 
5. When configuring scenes for the build, ensure that `AzureSpatialAnchorsDemoLauncher` is at index 0.

**NOTE:** The **SpatialAnchorConfig** file can be used in your own projects to share service credentials across scenes. When this file is used, you do not need to set these values on each **SpatialAnchorManager** in each scene. It's also possible to ignore this file in source control to avoid checking credentials into your repository. 

## Building for Sharing
1. Navigate to `AzureSpatialAnchors.Examples/Resources`
2. Select **SpatialAnchorSamplesConfig**
3. Set `Base Sharing URL` to the address of your own instance of the sample sharing server. 

**NOTE:** These values are currently only used by the `AzureSpatialAnchorsLocalSharedDemo` scene. 

## Known Issues When Switching Platforms
1. When switching platforms you may see some errors in the editor remarking that certain materials failed to unload. These messges are benign and can be ignored.  
2. If you experience AR rendering issues on Mobile, in Unity select Assets->Reimport All

## Building for HoloLens

For the .net scripting backend, build like a HoloLens project.

### Known issues for HoloLens

For the il2cpp scripting backend, see this [issue](https://forum.unity.com/threads/httpclient.460748/).

The short answer to the workaround is to:

1. First make a mcs.rsp with the single line `-r:System.Net.Http.dll`. Place this file in the root of your assets folder.
2. Copy the `System.net.http.dll` from `<unityInstallDir>\Editor\Data\MonoBleedingEdge\lib\mono\4.5\System.net.http.dll` into your assets folder.

There is an additional issue on the il2cpp scripting backend case that renders the library unusable in this release.

## Building for iOS

Import the Unity ARKit Plugin:

1. Download [Unity ARKit Plugin](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/downloads/?tab=tags) 2.0.0 from [here](https://bitbucket.org/Unity-Technologies/unity-arkit-plugin/get/v2.0.0.zip)
   and extract it locally.
2. Copy the contents of the `Assets` folder from the extracted Unity ARKit Plugin to the sample's `Assets` folder.

Nothing special when creating the Xcode project (compared to other ARKit projects)

After your first build:

1. From the exported project folder, run `pod install` to install the necessary CocoaPods.
2. Open `Unity-iPhone.xcworkspace` in Xcode.
3. Enable Automatic Signing in 'General' project settings (or whatever you need to do for signing).
4. Ensure deployment target is > 11.0.

## Building for Android

1. Download and import the ARCore SDK for Unity package. See [here](https://developers.google.com/ar/develop/unity/quickstart-android#get_the_arcore_sdk_for_unity) for detailed instructions, but essentially:
    1. Download [ARCore SDK for Unity](https://github.com/google-ar/arcore-unity-sdk/releases).
    2. Select **Assets > Import Package > Custom Package**
    3. Select the `arcore-unity-sdk-vX.X.X.unitypackage` file you previously downloaded.
2. Export the project
    1. Open **Build Settings** by selecting **File > Build Settings...**.
    2. Select the **Export** button and select a location to export the Android project to.
    3. Open the project in Android Studio, build, and deploy.

### Known issues for Android

- We are aware of a compatibility issue that prevents anchors from working on apps that depend on the recently released
  ARCore 1.6.0. At this time, we recommend customers remain on ARCore 1.5.0 or lower until a fix is available.

- You may encounter an error regarding multiple references to System.Net.Http. Refer the the
  [GitHub issue](https://forum.unity.com/threads/httpclient.460748/) for more details.
  The workaround is to:
  1. Make a file named `mcs.rsp` with the single line `-r:System.Net.Http.dll` in the root of your assets folder.

- You may encounter an error regarding a missing NDK toolchain for `mips64el-linux-android` when building the app.
  Refer to the [issue](https://github.com/google/filament/issues/15#issuecomment-426259512) for more details.
  The workaround is to:

  1. Create a folder for the missing\deprecated mips64el-linux-android toolchain:
       - On Windows:
         ``` cmd
          mkdir %ANDROID_HOME%\ndk-bundle\toolchains\mips64el-linux-android\prebuilt\windows-x86_64
         ```

## AAD user token scenario support for HoloLens

Instead of using an account key it's possible to acquire an AAD token and pass that into the SDK. For this we'll need to use the `Microsoft.IdentityModel.Clients.ActiveDirectory` library for the authentication. 

1. Because there is no NuGet packaging system in Unity, you'll need to manually download the library and add the proper dll to your Assets folder. Steps for downloading nuget packages can be found [here](https://docs.microsoft.com/en-us/nuget/consume-packages/ways-to-install-a-package).
 
2. Once the package has been downloaded, copy the files from `Microsoft.IdentityModel.Clients.ActiveDirectory\4.5.0\lib\uap10.0` into your project under `Assets\Plugins\WSA`. Using the `Plugins\WSA` folder should automatically mark the library to only be used for Windows.

3. Next, open the file `AzureSpatialAnchors.SDK\SpatialAnchorManager.cs` and scroll down to the method `GetAADTokenAsync`.

4. Comment out the line that throws the `NotSupportedException`.

5. Uncomment the remaining lines in the `GetAADTokenAsync` method. (These lines use the library we just downloaded to acquire the token.)

6. Update each **SpatialAnchorManager** in your project (or update **SpatialAnchorConfig** in the `AzureSpatialAnchors.SDK/Resources` folder) to use AAD, then specify your Client ID and Tenant ID.
