# Godot Outlines

A way to draw the outlines of 3D objects.

> The method used here **only works with Godot 4.3 and later**, because it uses the Compositor and CompositorEffect features. It also should **only work with the Forward+ renderer**, because it makes use of compute shaders.

## Features

### Currently available

- **Pixel perfect** outlines for **any 3D mesh**
- Per-node outline color
- Smooth and anti-aliased
- Optional "glow" effect to the outlines
- **Very, very fast** (can draw outlines of any size with very little performance cost)
- Modify the outlines settings at runtime

### On the way

- Display the outlines on any non-fullscreen camera
- *Hide outlines that are hidden behind other meshes?*

## Usage

> You can download this repo and open it as a Godot project to check out the ideal setup.

### Copy the files

Download and copy the `scripts` and `assets` folders to your project. If you ever want to change the location of the assets in your project, you will need to update their location in the scripts. The paths are stored as constants in the files that use them, so they should be easy to find.

> The scripts located in `scripts/outlines_demo` were created for the purpose of testing and showcasing the system. Feel free to remove them from your projet, because your probably won't need them. If however you decide to use them, note that they will require configuring the following inputs in the input map:
> - ToggleOutlines
> - AddOutlineable
> - RemoveOutlineable
> - SwitchCamera
> - SwitchEffect

Don't forget to **build the projet** before moving on to the next steps, otherwise the nodes won't show up in the "Create new node" menu.

### Select the nodes to outline

To outline 3D meshes, you will need to use the `OutlinerComponent` node. Place it anywhere in a scene, and set its "Target" exported variable to the node that you want to outline. Note that the `OutlinerComponent` will also outline all children of the selected node, so there is no need to use multiple `OutlinerComponent`s if you need to outline an object made of multiple meshes.

The `OutlinerComponent` node has a property named `Enabled` that you can toggle on and off in order to activate and deactivate the outline of the object.

### Draw the outlines

In order to actually see the outlines, you will simply need to add a `OutlinesDisplayComponent` node to the same scene as your `Camera3D`, and fill in its "Camera" exported variable.

Once this is done, everything should be working correctly!
