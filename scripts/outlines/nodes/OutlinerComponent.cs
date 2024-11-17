using Godot;
using Godot.Collections;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinerComponent : Node
	{
		private const string OUTLINEABLE_FOLDER = "res://assets/outlines/outlineable";
		private const string OUTLINEABLE_MATERIAL_PATH = $"{OUTLINEABLE_FOLDER}/outlineable.tres";
		private ShaderMaterial _OutlineableMaterial = null;

		private StringName _OutlineableGroup = null;
		private readonly Dictionary<MeshInstance3D, MeshInstance3D> _Outlineables = [];

		private void Setup(Node node)
		{
			// Do not create an outlineable mesh for outlineable meshes (risk of infinite recursion)
			if (node.IsInGroup(this._OutlineableGroup))
			{
				return;
			}

			// Setup the children of the node to setup
			foreach (Node child in node.GetChildren())
			{
				this.Setup(child);
			}

			// If the node to setup is a mesh, then create an outlineable mesh for it
			if (node is MeshInstance3D meshInstance)
			{
				MeshInstance3D outlineable = new();
				this._Outlineables[meshInstance] = outlineable;
				outlineable.Mesh = meshInstance.Mesh;
				outlineable.MaterialOverride = this._OutlineableMaterial;
				outlineable.Layers = this._OutlinesLayer;
				outlineable.AddToGroup(this._OutlineableGroup);
				node.AddChild(outlineable);

				if (this._Enabled)
				{
					outlineable.Show();
				}
				else
				{
					outlineable.Hide();
				}
			}

			// When a node is added as a child to the node to setup, setup that new node
			node.ChildEnteredTree += this.Setup;
			// When a child of the node to setup is removed, cleanup that former child
			node.ChildExitingTree += this.Cleanup;
		}

		private void Cleanup(Node node)
		{
			// No need to cleanup outlineable meshes, since they don't have children and none of their signals are listened to
			if (node.IsInGroup(this._OutlineableGroup))
			{
				return;
			}

			// Stop listening to the signals
			node.ChildEnteredTree -= this.Setup;
			node.ChildExitingTree -= this.Cleanup;

			// Remove the associated outlineable
			if (node is MeshInstance3D meshInstance && this._Outlineables.TryGetValue(meshInstance, out MeshInstance3D outlineable))
			{
				meshInstance.RemoveChild(outlineable);
				outlineable.QueueFree();
				this._Outlineables.Remove(meshInstance);
			}

			// Cleanup the children of the node to cleanup
			foreach (Node child in node.GetChildren())
			{
				this.Cleanup(child);
			}
		}

		[Export]
		private Node _Target = null;
		public Node Target
		{
			get => this._Target;
			set
			{
				this.Cleanup(this._Target);
				this.Setup(value);

				this._Target = value;
			}
		}

		[Export]
		private bool _Enabled = false;
		public bool Enabled
		{
			get => this._Enabled;
			set
			{
				foreach (MeshInstance3D outlineable in this._Outlineables.Values)
				{
					if (value)
					{
						outlineable.Show();
					}
					else
					{
						outlineable.Hide();
					}
				}

				this._Enabled = value;
			}
		}

		[ExportCategory("Outlines Settings")]
		[Export(PropertyHint.ColorNoAlpha)]
		private Color _OutlinesColor = Colors.White;
		public Color OutlinesColor
		{
			get => this._OutlinesColor;
			set
			{
				this._OutlineableMaterial.SetShaderParameter("outlines_color", value);
				this._OutlinesColor = value;
			}
		}

		[ExportCategory("Technical Settings")]
		[Export(PropertyHint.Layers3DRender)]
		private uint _OutlinesLayer = (uint)Mathf.Pow(2.0f, 19.0f);
		public uint OutlinesLayer
		{
			get => this._OutlinesLayer;
			set
			{
				foreach (MeshInstance3D outlineable in this._Outlineables.Values)
				{
					outlineable.Layers = value;
				}

				this._OutlinesLayer = value;
			}
		}

		public override void _Ready()
		{
			base._Ready();

			this._OutlineableGroup = $"Outlineable_{this.GetInstanceId()}";
			this._OutlineableMaterial = ResourceLoader.Load<ShaderMaterial>(OUTLINEABLE_MATERIAL_PATH);

			this.OutlinesLayer = this._OutlinesLayer;
			this.OutlinesColor = this._OutlinesColor;
			this.Enabled = this._Enabled;

			if (this._Target != null)
			{
				this.Setup(this._Target);
			}
		}

		public override void _ExitTree()
		{
			base._ExitTree();

			if (this._Target != null)
			{
				this.Cleanup(this._Target);
			}
		}
	}
}
