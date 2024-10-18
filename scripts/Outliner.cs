using Godot;

namespace Ppcs
{
	[GlobalClass]
	public partial class Outliner : Node
	{
		[Export]
		private Node MeshesParent = null;
		[Export]
		private int OutlineLayer = 20;

		private bool _Enabled = false;
		public bool Enabled
		{
			get => _Enabled;
			set
			{
				if (_Enabled == value)
				{
					return;
				}

				foreach (Node node in GetTree().GetNodesInGroup("Outlinable" + GetInstanceId().ToString()))
				{
					if (node is VisualInstance3D visualInstance)
					{
						if (value)
						{
							visualInstance.Layers |= (uint)Mathf.Pow(2, OutlineLayer - 1);
						}
						else
						{
							visualInstance.Layers &= ~(uint)Mathf.Pow(2, OutlineLayer - 1);
						}
					}
				}

				_Enabled = value;
			}
		}

		private void RegisterNode(Node node)
		{
			if (node is VisualInstance3D visualInstance)
			{
				visualInstance.AddToGroup("Outlinable" + GetInstanceId().ToString());
			}
		}

		private void UnregisterNode(Node node)
		{
			if (node.IsInGroup("Outlinable" + GetInstanceId().ToString()))
			{
				node.RemoveFromGroup("Outlinable" + GetInstanceId().ToString());
			}
		}

		private void WatchNode(Node node)
		{
			RegisterNode(node);
			node.ChildEnteredTree += RegisterNode;
			node.ChildExitingTree += UnregisterNode;

			foreach (Node child in node.GetChildren())
			{
				WatchNode(child);
			}
		}

		public override void _Ready()
		{
			base._Ready();

			if (MeshesParent == null)
			{
				return;
			}

			WatchNode(MeshesParent);
		}
	}
}
