using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinerComponent : Node
	{
		[Export(PropertyHint.Range, "1,20,1")]
		public int OutlineLayer { get; set; } = 20;

		private string _OutlinerGroupName = "";

		private void RegisterNode(Node node)
		{
			node.ChildEnteredTree += this.RegisterNode;
			node.ChildExitingTree += this.UnregisterNode;

			if (node is VisualInstance3D meshInstance)
			{
				meshInstance.AddToGroup(this._OutlinerGroupName);
			}

			foreach (Node child in node.GetChildren())
			{
				this.RegisterNode(child);
			}
		}

		private void UnregisterNode(Node node)
		{
			node.ChildEnteredTree -= this.RegisterNode;
			node.ChildExitingTree -= this.UnregisterNode;

			if (node.IsInGroup(this._OutlinerGroupName))
			{
				node.RemoveFromGroup(this._OutlinerGroupName);
			}

			foreach (Node child in node.GetChildren())
			{
				this.UnregisterNode(child);
			}
		}

		private Node _NodesToOutline = null;
		[Export]
		public Node NodesToOutline
		{
			get => this._NodesToOutline;
			set
			{
				if (Engine.IsEditorHint())
				{
					this._NodesToOutline = value;
					return;
				}

				if (this._NodesToOutline != null)
				{
					this.UnregisterNode(this._NodesToOutline);
				}

				if (value != null)
				{
					this.RegisterNode(value);
				}

				this._NodesToOutline = value;
			}
		}

		public override void _Ready()
		{
			base._Ready();

			this._OutlinerGroupName = $"Outliner_{this.GetInstanceId()}";

			if (this._NodesToOutline != null)
			{
				this.RegisterNode(this._NodesToOutline);
			}
		}

		private bool _Enabled = false;
		public bool Enabled
		{
			get => this._Enabled;
			set
			{
				if (this._Enabled == value)
				{
					return;
				}

				foreach (Node node in GetTree().GetNodesInGroup(this._OutlinerGroupName))
				{
					if (node is VisualInstance3D visualInstance)
					{
						if (value)
						{
							visualInstance.SetLayerMaskValue(this.OutlineLayer, true);
						}
						else
						{
							visualInstance.SetLayerMaskValue(this.OutlineLayer, false);
						}
					}
				}

				this._Enabled = value;
			}
		}
	}
}
