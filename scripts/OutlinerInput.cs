using Godot;

namespace Ppcs
{
	[GlobalClass]
	public partial class OutlinerInput : Node
	{
		[Export]
		private Outliner Outliner = null;

		public override void _Input(InputEvent inputEvent)
		{
			if (inputEvent.IsActionPressed("ToggleOutlines"))
			{
				Outliner.Enabled = !Outliner.Enabled;
			}
		}
	}
}
