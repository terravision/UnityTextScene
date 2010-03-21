DISCLAIMER: Use this code at your own risk! It is WIP!

----What is this?

The TextScene system is intended to be a complete replacement of the Unity built-in binary scene format. The reason to hack together and use such a format, is because it is very difficult, if not impossible, to merge scene conflicts in a team environment using a binary scene format. If you don't care about diffable scenes, you should avoid this (one-man teams will most likely not benefit at all from this, unless you desperately want scene history).

If you decide to venture into the source code, be warned that it is poorly commented and work in process.

----How does this stuff work?

When using TextScenes, you are highly advised against mixing them with binary Unity scenes. This is because the TextScene system does a few things in parallell with or re-implements (badly, mind you) certain functionality from the binary Unity scene handling. What immediately comes to mind is the Hierarchy View and the build process.

While you will primarily work with text-based scene files, the binary unity scenes are still used as temporary files. The TextScene files are only used on save and load, and then saved to temporary unity scenes outside your assets folder. These files are also used when a player is built (this way scene dependencies should work as they do using the regular binary scene format).

The TextScene format requires certain changes to the normal workflow in Unity. First of all, as far as I know, there are no useful hooks for the GUI save and load exposed to editor scripts, so you should no longer use Load or Save from the main menu (in fact, Save *should* work, as the TextSceneMonitor should pick that one up, but it is recommended to keep with the TextScene functionality). To open a TextScene, please use the menu TextScene or right-click a TextAsset and choose 'TextScene Open'. It is also possible to open TextScenes via the TextScene build settings window, or via the in-scene TextScenes' inspector (they have a component called TextSceneObject).

When you are working on a TextScene, it is recommended to keep the TextSceneHierarchy view open at all times. This hierarchy has added functionality in order to prevent user mistakes, such as disabling children of TextSceneObjects. It also regularly pokes the TextSceneMonitor so it is able to keep monitoring our files.

To get started, open the project. Go to the TextScene menu and choose "Hierarchy". You should now be told to close down the standard hierarchy, and it is advised to do so. You will also very likely be prompted with a file dialog. Click cancel and click the next dialog which tells you to save using the TextScene menu. When you intend to create new scenes, it is recommended to not click away the file dialog, but save to a TextScene to keep the TextSceneMonitor happy. However, now expand the "Scenes" folder in the Project view. Right-click 'scenedemo' and choose TextScene Open. You should now be looking at a scene containing a few primitives.

As you can see, there are both blue and yellow colored objects in the hierarchy. The yellow-colored object is a TextSceneObject and it is in reality a link to another TextScene. If you open 'demoscene' in a text editor, you will see that the 'sceneinscene' object is only a transform and a pointer to a different TextScene. Select the yellow object in the hierarchy, and click "sceneinscene" on the button that should appear in the Inspector. Choose "yes", and you should now have navigated to the TextScene the link points at. Drag the MyColor-script from "Scripts/General" onto the ScaledSphere, set the color to something other than red or white, and save the scene. Go back to the main scene by right-clicking 'demoscene' in the Project view, and hit 'play'. If all goes well, it should now start up and the scaled sphere should turn into whatever color you set it to. 

If you expand the yellow objects, you will notice that the children are uneditable. This is to prevent any changes in the scene that will not persist when you save and load (there is no "apply" functionality for TextSceneObjects, unfortunately). Now, select the Capsule prefab. Notice that you can drag the root prefab and root TextSceneObject around in the hierarchy, but you cannot move children out, for the same save/load reason.

At last, go to TextScene again and go to Build->Open Window. This is where you should create builds from. Hopefully, demoscene should be in the list. Feel free to build a player to verify that it actually works.


----What are the main elements?

*TextSceneSerializer:

This class writes the current scene to a human-readable, diffable textfile. If it fails, it will give the user feedback (hopefully) and not write anything to the actual file until the errors have been resolved.

*TextSceneDeserializer:

Reads a TextScene file and populates a scene (either a clean one, or loads a scene into an existing scene).

*TextSceneMonitor:

Is polled continously by the TextSceneHierarchy. This class monitors user actions and external file changes. When a scene is loaded, the TextSceneDeserializer notifies this class and tells it to create binary temporary representations of the scene (note: The implementation for this is extremely hacky, but works for now. It is related to what seems like a bug with the Unity API SaveScene/LoadScene, see code for details). These temporary scenes are standard Unity scenes, and they are also what we pass on to the player builder. These scenes are regularly monitored for changes, and this is currently how we try to detect if the user saved using built-in functionality. The binary temp scenes are stored outside the Assets folder, like this:  /Project/TempScenes/somefilename.unity, in parallell with their TextScene counterpart, which is in /Project/Assets/somefilename.txt

The TextSceneMonitor also monitors editor state changes. The only one we actually react upon, is whenever the user presses "play". The TextScene system will then validate any BuildSettings in order to make the in-editor play session as painless as possible. Keep in mind that we are using TextScenes which need to be converted to binary Unity scenes before they are usable, so part of the validation process is making sure these actually exist and are up to date.

*TextSceneHierarchy

A poor replacement of the built-in Scene Hierarchy. It has the most basic functionality, such as moving stuff around the tree, new color codes for TextSceneObjects, the ability to add TextScenes into scenes (can be thought of as a way of doing prefabs-in-prefabs - drag'n'drop a TextAsset containing a TextScene into the view pane, the result should be a yellow-colored object containing the other scene), instantiating objects by drag'n'drop from the Project View and script assignment.

*TextSceneWindow (stupid name, this is in fact the build settings)

Exposes TextScene build functionality to the user. Most of the functionality itself is implemented in TextScene (which basically is a utility class).

*TextScene

Utility class that most importantly has functionality for reading/writing TextSceneBuildSettings and creating builds.


----What are the main limitations and areas that desperately need work?

In random order:

*Pro is required (at least for the build stuff, not sure about how/if the asset reference GUID resolves will break on Unity 'regular' or using the Asset Server - the current project has External VCS enabled).

*Currently, it is not possible to save and load prefabs *with instance changes*. Such changes will simply be ignored when saving, so they are not preserved.

*Certain Unity objects cannot be entirely reconstructed from scripts, for example the ellipsoid particle emitter. Such objects should be made into prefabs and dropped into the scene for TextScene compatibility.

*The TextSceneHierarchy view has very limited functionality compared to the built in hiererchy. Multiple selection, keyboard shortcuts, object renaming is just a fraction of the missing features.

*In-scene links (typically drag'n'drop links between components) require the linked object to have a unique path in the scene. The reason for this, is that the only reference stored in the TextScene is the full scene path of the object. Using InstanceID is a possible way to get around this, but is currently not implemented.

*The textscene format currently lists the count of children, components, script members, array sizes etc in the format itself. This should be removed, as it makes hand-editing the scene files more difficult than it should be.

*I have seen at a couple of occasions that Unity gives strange error messages regarding temp file overwrites. I have yet to figure out exactly why or when this happens, but I have not (yet) seen any loss of data - it happens to the temporary binary files, not the TextScenes themselves.

*TextSceneObjects (scene-in-scene links) are not monitored for external changes.

*Prefabs seem to not revert correctly. The reverts arent 'recursive', so if you instantiate a prefab, move it's instance child around, select the root instance again and click 'revert' - nothing happens. You have to select the child itself and 'revert' that one. Given that instance changes to prefabs isn't currently supported, changes like this will automatically revert themselves when you save and re-load a scene. You have been warned ;)

*The context-menu item 'Disconnect' in the TextSceneHierarchy is badly implemented - it re-instantiates a new object and deletes the prefab instance, resulting in loss of in-scene links if any objects were pointing at the prefab instance.

*Code cleanups. Also look for TODO/FIXME tags in the code.

*Probably more.

Good luck, and thanks for trying it out!

----About UnityTextScene
UnityTextScene was an internal project at serious games studio TerraVision (http://www.terravision.no), which was decided to be shared with the Unity community, for further development.

