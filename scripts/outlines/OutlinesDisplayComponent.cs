using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinesDisplayComponent : Node
	{
		[Export]
		public SubViewport OutlinesCaptureViewport { get; set; } = null;
		[Export]
		public Camera3D OutlinesCaptureCamera { get; set; } = null;
		[Export]
		public TextureRect OutlinesDisplayRect { get; set; } = null;

		private void SetupCaptureViewport()
		{
			Vector2I mainViewportSize = Vector2I.Zero;
			Viewport mainViewport = this.GetViewport();
			if (mainViewport is Window window)
			{
				mainViewportSize = window.Size;
			}
			else if (mainViewport is SubViewport subViewport)
			{
				mainViewportSize = subViewport.Size;
			}
		}
	}
}
