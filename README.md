# EditorVR
Author VR in VR - Initial public release was on December 15, 2016 via [blogpost](https://blogs.unity3d.com/2016/12/15/editorvr-experimental-build-available-today/)

## Getting Started
If you've made it here, but aren't accustomed to using GitHub, cloning repositories, etc. and are simply looking to give EditorVR a spin, then take a look at the [Getting Started Guide](https://docs.google.com/document/d/1IFQve5gAOb1gQzIhhtEr3WLrctJhsoJxl6j07pg2DYA/edit). Once you're up and running we recommend you join the discussion on the [EditorVR forum](https://forum.unity3d.com/forums/editorvr.126/).

## For Software Developers
If you're a developer, we recommend that you take a look at the [Getting Started Guide](https://docs.google.com/document/d/1IFQve5gAOb1gQzIhhtEr3WLrctJhsoJxl6j07pg2DYA) *and* the companion document [Extending EditorVR](https://docs.google.com/document/d/1EGi9hKXAujfBMI2spErojdqRc0giqEnOu0NpwgBxtpg). You'll need to clone the repository into an existing project using the instructions below.

### Git Dependencies
- [git-lfs](https://git-lfs.github.com/)
- [git-submodule](https://git-scm.com/docs/git-submodule)

### Cloning
1. Create a new Unity project or use an existing one
2. From the command line change directory to your project's `Assets` directory.
3. Run `git lfs clone --recursive -b development https://github.com/Unity-Technologies/EditorVR`

### Project Settings
If you plan on making changes to EditorVR and/or contributing back, then you'll need to set the `Asset Serialization` property under Edit->Project Settings->Editor to `Force Text`
