# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.4.1-preview] - 2020-01-03
Update dependencies and finalize package manager release

## [0.4.0-preview.3] - 2019-12-18
-- Clean up import warnings
-- Add dependencies on timeline and Text Mesh Pro

## [0.4.0-preview.2] - 2019-11-14
-- Fix settings menu issues
-- Fix ViewerScaleVisuals Line Renderer

## [0.4.0-preview.1] - 2019-12-08
-- Fixes related to immutable package exceptions
-- Upgrade to latest VRLineRenderer

## [0.4.0] - 2019-11-14
-- Refactor to use Module Loader package
-- Add package manifest, change structure to match package layout

## [0.3.0] - 2019-11-05
-- Two-handed rotate-and-scale
-- Remove partner SDK dependencies
-- Misc fixes

## [0.2.1] - 2019-03-05
-- EditorXR Runtime Fixes
  -- Context manager gets destroyed (#530)
  -- Mesh has been marked as non-accessible (#531)
  -- Serialized class layout errors, etc. (#544)
-- Fix warnings for .NET 4.x runtime (#539)
-- Fix serialization for Unity versions <= 2018.1

## [0.2.0] - 2019-02-23
-- EditorXR Runtime
  -- EditorXR works in play mode (subset of functionality)
  -- EditorXR works in player builds (smaller subset of functionality)
  -- Update to use assembly definitions, option to exclude in player builds
  -- Known issue: Context manager gets destroyed
  -- Known issue: Mesh has been marked as non-accessible
-- Spatial Menu
  -- One-handed menu control that is accessible independent of the user's viewpoint, or controller visibility
  -- Access most tools, workspaces, and actions that EditorXR provides
  -- GazeDivergenceModule + AdaptivePositionModule for lazy follow, and position adjustment based on gaze and HMD motion.
-- Unity 2018.3 support
-- Annotation tool upgrade
-- Minimal context
  -- Ideal for lightweight client builds not requiring much of the EditorXR UI.
  -- Unobtrusive context with no tools or menus other than the Spatial Menu.
  -- Improvements to context handling (can hide workspaces, define preferences per context-type)
-- Auto-open option (start EditorXR when the user puts on the HMD)
-- Update UI to use Text Mesh Pro
-- Mouse locomotion
-- Floor indicator
-- Drag and drop improvements
-- Performance and bug fixes

## [0.1.1] - 2017-12-11
-- Bug fixes
-- Undo menu
-- Serialized feedback editor (edit/view # of presentations)

## [0.1.0] - 2017-11-30
-- PolyWorkspace and PolyModule - Access Google's Poly API in EditorXR
-- WebModule - Make web requests that wait in a queue if there are too many simulataneous requests

## [0.0.9] - 2017-11-22
-- Experimental build no longer required! Find the recommended Unity build for EditorXR here
-- Controllers project - Dynamic tooltips and visual hints have been added to the controllers to give context and discoverability when using any EXR feature.
  -- UI Gaze detection
  -- Controllers are now semi-transparent, shake to reveal them at full opacity
  -- Users can customize any aspect of this system, from new controllers to tools with feedback
-- Text readability greatly improved through TextMeshPro
-- Block Selection
-- UI supports scenes with fog enabled
-- Fast toolswapping via spatial workflow
Known issues:
-- Performance improvements are an ongoing priority
-- Console + profiler can�t be resized
-- Single Pass Stereo + EXR is currently non-functional with Unity 2017.2 and .3

## [0.0.8] - 2017-10-01
-- New workflow to quickly switch between different tools
-- Annotation tool to draw and leave notes in a scene
-- Locomotion tool improvements including blink snapping
Known issues:
-- The MiniWorld workspace is currently not working in single pass

## [0.0.7] - 2017-06-05
-- Ray input is now forwarded to traditional GUI windows (e.g. Profiler / Console)
-- Highlight looks correct now in the MiniWorld
Known issues:
-- The MiniWorld workspace is currently not working in single pass

## [0.0.6] - 2017-05-12
-- 5.6 upgrade (requires new custom EditorVR 5.6 build)
-- Single pass rendering support
Known issues:
-- The MiniWorld workspace is currently not working in single pass
-- Grabbing objects out of the MiniWorld causes exceptions (already fixed in next release)

## [0.0.5] - 2017-04-21
Final 5.4 release (requires custom EditorVR 5.4 build)
-- Workspace manipulation improvements**
  -- If you have a workspace that is inheriting from Workspace and not IWorkspace, then you will need to assign the Assets/EditorVR/Workspaces/Base/ActionMaps/WorkspaceInput.asset to the Action Map field.
-- VR editing contexts (advanced dev feature)
-- Default tools can be replaced by creating new EditorVR Contexts
(Assets->Create->EditorVR->EditorVR Context) or copy EditorVR.asset and modify
** Note: Grab and resize now use the secondary trigger (grip on Vive)

## [0.0.4-p1] - 2017-04-16
Patch release to fix not being able to grab the player head in the MiniWorld

## [0.0.4] - 2017-04-11
-- Snapping
-- Locking
-- Workspace layout save
-- Interface methods no longer require implementation for injection (breaking change)

## [0.0.3] - 2017-03-24
-- Highlight now uses an outline
-- Drag/drop to re-arrange hierarchy
-- Hierarchy filters

## [0.0.2] - 2017-03-10
-- Viewer scaling of world (using two-handed secondary triggers)
-- Radial menu improvements (stays hidden mostly)
-- Tooltips

## [0.0.1] - 2017-03-03
### This is the first release of *Editor VR*
