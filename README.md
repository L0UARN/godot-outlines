# Godot Outlines

A way to outline meshes in Godot 4+ games.

## Usage

In order to start outlining meshes, you'll need two things: a way to indicate which nodes are to be outlined, and a way to display the outlies.

The `Outliner` node is what's needed to indicate the outlined nodes. Its `MeshesParent` exported field must be set to the node which contains the meshes needing to be outlined. The `Enabled` property can be toggled on or off to enable the outlines on the selected meshes. By default, `Enabled` is set to `false`.

The `OutlineDisplay` node is what's needed to actually display the outlines on screen. This node requires 3 other nodes: a `SubViewport`, a `Camera3D` and a `TextureRect`. The `Camera3D` should be a child of the `SubViewport`, and the `TextureRect` should be made to cover the whole screen so that the outlines match the meshes. I recommend you create a scene from the `OutlineDisplay` and its dependencies, so that you can make it autoload in the project settings.

The ideal node structure can be seen in the `testing.tscn` file of this example project.
