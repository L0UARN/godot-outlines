using System;
using Godot;
using Godot.Collections;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinerComponentDemo : Node
	{
		[Export]
		public OutlinerComponent OutlinerComponent = null;
		private Array<MeshInstance3D> AddedOutlineables = new();

		private void AddOutlineable()
		{
			MeshInstance3D newOutlineable = new();
			AddedOutlineables.Add(newOutlineable);
			newOutlineable.Mesh = new BoxMesh();
			OutlinerComponent.Target.AddChild(newOutlineable);

			newOutlineable.GlobalPosition = new(
				4.0f - Random.Shared.NextSingle() * 8.0f,
				1.0f - Random.Shared.NextSingle() * 2.0f,
				4.0f - Random.Shared.NextSingle() * 8.0f
			);

			newOutlineable.GlobalRotation = new(
				Random.Shared.NextSingle() * Mathf.Pi * 2,
				Random.Shared.NextSingle() * Mathf.Pi * 2,
				Random.Shared.NextSingle() * Mathf.Pi * 2
			);
		}

		private void RemoveOutlineable()
		{
			if (AddedOutlineables.Count > 0)
			{
				AddedOutlineables[0].QueueFree();
				AddedOutlineables.RemoveAt(0);
			}
		}

		public override void _Input(InputEvent inputEvent)
		{
			base._Input(inputEvent);

			if (inputEvent.IsAction("ToggleOutlines") && inputEvent.IsPressed())
			{
				OutlinerComponent.Enabled = !OutlinerComponent.Enabled;
			}
			else if (inputEvent.IsAction("AddOutlineable") && inputEvent.IsPressed())
			{
				this.AddOutlineable();
			}
			else if (inputEvent.IsAction("RemoveOutlineable") && inputEvent.IsPressed())
			{
				this.RemoveOutlineable();
			}
		}
	}
}
