using Godot;
using Godot.Collections;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinerComponent : Node
	{
		private const string OUTLINEABLE_FOLDER = "res://assets/outlines/outlineable";
		private const string OUTLINEABLE_MATERIAL_PATH = $"{OUTLINEABLE_FOLDER}/outlineable.tres";

		private StringName _SetupNodesGroup = null;
		private ShaderMaterial _OutlineableMaterial = null;
		private Dictionary<Node, MeshInstance3D> _OutlinesMeshes = [];

		private void CleanupTarget(Node target)
		{
			if (target == null)
			{
				return;
			}

			// Disconnect the signals if they haven't already been disconnected and if they have been connected
			if (target.IsInsideTree() && target.IsInGroup(this._SetupNodesGroup))
			{
				target.ChildEnteredTree -= this.PrepareTarget;
				target.ChildExitingTree -= this.CleanupTarget;
			}

			// If the target has an outline mesh setup, delete it
			if (this._OutlinesMeshes.TryGetValue(target, out MeshInstance3D outlinesMesh))
			{
				outlinesMesh.QueueFree();
				this._OutlinesMeshes.Remove(target);
			}

			// Recursively cleanup the children of the target
			foreach (Node child in target.GetChildren())
			{
				this.CleanupTarget(child);
			}
		}

		private void SetupTarget(Node target)
		{
			if (target == null)
			{
				return;
			}

			// Recursively setup the children of the target
			foreach (Node child in target.GetChildren())
			{
				this.PrepareTarget(child);
			}

			// If the target is a mesh, it needs to be setup for outlining
			if (!this._OutlinesMeshes.ContainsKey(target) && target is MeshInstance3D outlineable)
			{
				// Create a mesh that will only be seen by the outlines camera
				MeshInstance3D outlinesMesh = new()
				{
					Mesh = outlineable.Mesh,
					MaterialOverride = this._OutlineableMaterial,
					Layers = this.OutlinesLayer,
					Visible = this.Enabled
				};

				this._OutlinesMeshes[target] = outlinesMesh;
				target.AddChild(outlinesMesh);
			}

			// Setup the potential children of the target
			target.ChildEnteredTree += this.PrepareTarget;
			// Cleanup the children of the target when they exit the tree
			target.ChildExitingTree += this.CleanupTarget;
			// Mark the target to know that the signals have been connected
			target.AddToGroup(this._SetupNodesGroup);
		}

		private void PrepareTarget(Node target)
		{
			if (target == null)
			{
				return;
			}

			// Only setup the node if it's read
			if (target.IsNodeReady())
			{
				this.SetupTarget(target);
				return;
			}

			// If it's not ready, set it up after it gets ready
			target.Ready += () => this.SetupTarget(target);
		}

		[Export]
		private Node _Target = null;
		public Node Target
		{
			get => this._Target;
			set
			{
				if (value == this._Target)
				{
					return;
				}

				// Cleanup the previous target
				this.CleanupTarget(this._Target);
				// Setup the new target
				this.PrepareTarget(value);

				this._Target = value;
			}
		}

		private void UpdateColor(Color color)
		{
			this._OutlineableMaterial.SetShaderParameter("outlines_color", color);
		}

		[Export(PropertyHint.ColorNoAlpha)]
		private Color _OutlinesColor = Colors.White;
		public Color OutlinesColor
		{
			get => this._OutlinesColor;
			set
			{
				if (value.Equals(this._OutlinesColor))
				{
					return;
				}

				this.UpdateColor(value);
				this._OutlinesColor = value;
			}
		}

		private void UpdateEnabled(bool enabled)
		{
			foreach (MeshInstance3D outlinesMesh in this._OutlinesMeshes.Values)
			{
				outlinesMesh.Visible = enabled;
			}
		}

		[Export]
		private bool _Enabled = false;
		public bool Enabled
		{
			get => this._Enabled;
			set
			{
				if (value.Equals(this._Enabled))
				{
					return;
				}

				this.UpdateEnabled(value);
				this._Enabled = value;
			}
		}

		private void UpdateOutlinesLayer(uint outlinesLayer)
		{
			foreach (MeshInstance3D outlinesMesh in this._OutlinesMeshes.Values)
			{
				outlinesMesh.Layers = outlinesLayer;
			}
		}

		[Export(PropertyHint.Layers3DRender)]
		private uint _OutlinesLayer = (uint)Mathf.Pow(2.0f, 19.0f);
		public uint OutlinesLayer
		{
			get => this._OutlinesLayer;
			set
			{
				if (value.Equals(this._OutlinesLayer))
				{
					return;
				}

				this.UpdateOutlinesLayer(value);
				this._OutlinesLayer = value;
			}
		}

		public override void _Ready()
		{
			base._Ready();

			this._SetupNodesGroup = $"Outlineable_{this.GetInstanceId()}";
			ShaderMaterial originalOutlineableMaterial = ResourceLoader.Load<ShaderMaterial>(OUTLINEABLE_MATERIAL_PATH);
			this._OutlineableMaterial = (ShaderMaterial)originalOutlineableMaterial.Duplicate();

			this.UpdateOutlinesLayer(this._OutlinesLayer);
			this.UpdateEnabled(this._Enabled);
			this.UpdateColor(this._OutlinesColor);
			this.PrepareTarget(this._Target);
		}

		public override void _ExitTree()
		{
			base._ExitTree();

			// Cleanup what was created by the outliner
			this.CleanupTarget(this._Target);
		}
	}
}
