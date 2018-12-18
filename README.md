# AdvancedDynamicBoneEditor
A Unity editor script for vrchat avatar creators that supports more use cases when copying dynamic bone components to a new model or automatically fixing references on manual copies.

Tool Screenshot: [Imgur Link](https://imgur.com/yHUG9pw) (Beta v0.7.3)

## Change Log (Beta v0.7.4):
- Added scale correction when copying (radius, end offset, gravity, force) to a new dynamic bone
- When creating a missing gameobject during dynamic bone collider copy, it will also attempt to recreate any missing parent hierarchy for it to be childed correctly
- Fixed model scale being falsely detected as a mismatch due to floating point accuracy
- Fixed scale correction multiplier falsely detecting a mismatch due to floating point accuracy
- Output log is now categorised for both easier readability and to help avoid gui text length limitations
- Output log will now automatically expand categories that contain errors
- Added PumkinsAvatarTools v0.4b integration

## What Can ADBEditor Do:
- Copy with references from anyhwhere in the avatar hierarchy or straight from the root (see settings tab)
- Copy dynamic bones and dynamic bone colliders attached to the root
- Copy support for multiple dynamic bones or dynamic bone colliders on the same gameobject
- Copy support for dynamic bone colliders added to empty gameobjects childed anywhere
    - Will create missing gameobjects 
    - Will attempt to recreate the parent hierarchy when copying if one does not exist
- Copy support for incorrect scales while copying (enabled by default in settings tab)
  - Currently includes import, root , and local component gameobject scale correction
  - This fixes dynamic bone radius, end offset, gravity, and force which are affected by scale
  - This fixes dynamic bone collider radius and height which are affected by scale
- Failsafe copy that retains the original model references in the new model copy for quick fixing with the included fix references tool
- This editor comes with my fix references tool
  - Runs automatically after a copy of any dynamic bones or can be used manually
  - Fix references on manually copied dynamic bones for the Root, Exclusions and Colliders
  - Remove empty collider or exclusion references automatically
- Console and verbose output logging for the last run automatic sequence of tasks (see settings tab)
  - Provide information on unsuccessful tasks and why 
  - Provide information on user action required tasks after a copy
- Full undo support allowing you to simply undo an action if the result is undesired or you would like to use different settings
- Has simple integration support for [PumkinsAvatarTools](https://github.com/rurre/PumkinsAvatarTools). See screenshot: [Imgur Link](https://i.imgur.com/eEUbiRf.png)
  - For ease of use with other components you wish to copy

## Does Not Currently Support:
- Correct position offsets when copying between full body and non full body avatars
- Duplicate gameobjects are currently being detected and logged to the user, but no action is taken to correct the names automatically
- Copying any other component (unless you accept the pumkins tools integration )

**Important:** The release packages should do this automatically for you, but make sure that all scripts reside in an `Editor` folder anywhere in your project. 

**Disclaimer:** This tool is in beta and while it should let you know in the logs if something went wrong, not every use case is known and issues may occur. (please log an issue or contact me if something does go wrong so that I can fix or potentially support your use case). Remember though I always recommended backing up your avatars and use this tool at your own risk.
