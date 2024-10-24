using System;
using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinerComponent : Node
	{
		[Export(PropertyHint.Layers3DRender)]
		public int OutlineLayer { get; set; } = (int)Mathf.Pow(2, 19);

		private string _OutlinerGroupName = "";

		private void RegisterNode(Node node)
		{
			node.ChildEnteredTree += this.RegisterNode;
			node.ChildExitingTree += this.UnregisterNode;

			if (node is VisualInstance3D visualInstance)
			{
				visualInstance.AddToGroup(this._OutlinerGroupName);
				this.EnableOutlinesForNode(visualInstance, this._Enabled);
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
				this.EnableOutlinesForNode(node, false);
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
				if (Engine.IsEditorHint() || !this.IsNodeReady())
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

		private void EnableOutlinesForNode(Node node, bool enable)
		{
			int outlineLayer = (int)Math.Log2(this.OutlineLayer) + 1;

			if (node is VisualInstance3D visualInstance)
			{
				if (enable)
				{
					visualInstance.SetLayerMaskValue(outlineLayer, true);
				}
				else
				{
					visualInstance.SetLayerMaskValue(outlineLayer, false);
				}
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
					this.EnableOutlinesForNode(node, value);
				}

				this._Enabled = value;
			}
		}
	}
}
