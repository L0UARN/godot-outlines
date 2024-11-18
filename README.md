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

### On the way

- Better ease of use
- Display the outlines on any camera
- *Change the outline size and / or glow radius while running?*
- *Hide outlines that are hidden behind other meshes?*

## Usage

> You can download this repo and open it as a Godot project to check out the ideal setup.

### Copy the files

Download and copy the `script` and `assets` folders to your project. If you ever want to change the location of the assets in your project, you will need to update their location in the scripts.

### Select the nodes to outline

To outline 3D meshes, you will need to use the `OutlinerComponent` node. Place it anywhere in a scene, and set its "Target" exported variable to the node that you want to outline. Note that the `OutlinerComponent` will also outline all children of the selected node, so there is no need to use multiple `OutlinerComponent`s if you need to outline an object made of multiple meshes.

The `OutlinerComponent` node has a property named `Enabled` that you can toggle on and off in order to activate and deactivate the outline of the object.

### Draw the outlines

In order to actually see the outlines, you will need to setup a few things.

- Start by creating a new scene, with a `CanvasLayer` as a root node.
- Add a `SubViewport` node as a child of the `CanvasLayer`.
- Add a `Camera3D` node to the `SubViewport`.
- Add a `TextureRect` as a child of the `CanvasLayer`.
- Finally, add an `OutlinesDisplayComponent` node as a child of the `CanvasLayer`.

You do not need to change any of the settings of the nodes you just created, except for the `OutlinesDisplayComponent`. For each setting of the "Required nodes" section, select the matching one that you just created.

Once this is done, add the scene to your main scene (you could even add it to the autoloaded scenes, if you frequently need outlines). The outlines should be working now.
