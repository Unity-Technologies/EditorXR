# EditorXR
Author XR in XR - Initial public release was on December 15, 2016 via [blogpost](https://blogs.unity3d.com/2016/12/15/editorvr-experimental-build-available-today/)

## Experimental Status
It’s important to note that EditorXR is an experimental feature. As such, there is no formal support (e.g. FogBugz, support@unity3d.com, Premium Support, etc.) offered, so please do not use these channels. Instead, take your questions, suggestions, comments to our dedicated [forum](https://forum.unity3d.com/forums/editorvr.126/).

To help ensure you have a good experience, and to help us answer your questions (hey, we’re a small team!), we encourage you to try it out first with a small VR-ready scene. Please use life-sized objects, nothing too big or small. Dive in and have fun just playing around, instead of trying to use your existing project.

**As with any experimental/preview/alpha/beta build, it is always a good idea to make a backup of your project before using the build.**

Experimental means this:
- We're still adding features!
- The current menus, tools, workspaces, actions, etc. are not the end-all-be-all. Each of these have individual designs that will change as we experiment with what works best for UX. EditorXR was designed in such a way that we plan on you being able to replace all of these defaults, too, if you so desire.
- Namespaces, classes, software architecture, prefabs, etc. can change at any point. If you are writing your own tools, then you might need to update them as these things change.
- There won’t always be an upgrade path from one release to the next, so you might need to fix things manually, which leads to the next point...
- Stuff can and will break (!)
- There’s **no guarantee** that this project will move out of experimental status within any specific timeframe.
- As such, there is no guarantee that this will remain an actively supported project.

## Getting Started
If you've made it here, but aren't accustomed to using GitHub, cloning repositories, etc. and are simply looking to give EditorXR a spin, then take a look at the [Getting Started Guide](https://docs.google.com/document/d/1RD0SAjWnXdtY6eOC4qHk_fcl7w2-aBGrF3rX5pk5KDo). Once you're up and running we recommend you join the discussion on the [EditorXR forum](https://forum.unity3d.com/forums/editorvr.126/).

## For Software Developers
If you're a developer, we recommend that you take a look at the [Getting Started Guide](https://docs.google.com/document/d/1RD0SAjWnXdtY6eOC4qHk_fcl7w2-aBGrF3rX5pk5KDo) *and* the companion document [Extending EditorXR](https://docs.google.com/document/d/1EGi9hKXAujfBMI2spErojdqRc0giqEnOu0NpwgBxtpg). You'll need to clone the repository into an existing project using the instructions below.

### Git Dependencies
- [git-lfs](https://git-lfs.github.com/)
- [git-submodule](https://git-scm.com/docs/git-submodule)

### Project Asset Dependencies
- [Textmesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@1.2/manual/index.html#installation)
- [Legacy Input Helpers](https://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@1.0/manual/index.html#installing-comunityxrlegacyinputhelpers) (2019.1+)
  - Users of 2018.3 do not need Legacy Input Helpers

### Cloning
1. Create a new Unity project or use an existing one
2. From the command line change directory to your project's `Assets` directory.
3. Run `git lfs clone --recursive -b development https://github.com/Unity-Technologies/EditorXR` **(Use HTTPS!)**

### Updating
Because this project uses [git-submodule](https://git-scm.com/docs/git-submodule), you'll need to execute `git submodule update` after pulling whenever a submodule is updated. You could execute this command always just to be safe or if you notice that a submodule is showing as modified after pulling changes.

Optionally, you could add a [git hook for post-checkout](https://ttboj.wordpress.com/2014/05/06/keeping-git-submodules-in-sync-with-your-branches/) or use a GUI (e.g. SourceTree) that does this automatically for you.

### Project Settings
If you plan on making changes to EditorXR and/or contributing back, then you'll need to set the `Asset Serialization` property under Edit->Project Settings->Editor to `Force Text`.

### Assembly Definitions
In order to support a variety of platform configurations, and to optionally strip its code out of player builds, EditorXR uses assembly definitions. Some of EditorXR's dependencies do not include assembly definitions in their current forms, so after importing EditorXR (in Unity 2018.3 and below), you must add them.

For easy set-up, EditorXR includes a .unitypackage (`Patches/Dependencies_asmdef.unitypackage`) containing an assembly definition for the PolyToolkit and UnityEngine.SpatialTracking, which are referenced by EditorXR. Simply import it via Assets > Import Package > Custom Package...

This is not required for Unity versions 2019.1 and above, though you will need to add an assembly definition in order to reference PolyToolkit.

## All contributions are subject to the [Unity Contribution Agreement (UCA)](https://unity3d.com/legal/licenses/Unity_Contribution_Agreement)
By making a pull request, you are confirming agreement to the terms and conditions of the UCA, including that your Contributions are your original creation and that you have complete right and authority to make your Contributions.
