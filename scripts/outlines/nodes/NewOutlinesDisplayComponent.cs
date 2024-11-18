using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class NewOutlinesDisplayComponent : Node
	{
		[Export]
		public Camera3D Camera { get; set; } = null;

		[ExportCategory("Outlines settings")]
		[Export]
		public int OutlinesSize { get; set; } = 4;
		[Export]
		public int GlowRadius { get; set; } = 2;

		[ExportCategory("Technical settings")]
		[Export(PropertyHint.Range, "0.5,1.0,0.05")]
		public float OutlinesRenderScale { get; set; } = 1.0f;
		[Export(PropertyHint.Layers3DRender)]
		public uint OutlineLayer { get; set; } = (uint)Mathf.Pow(2.0f, 19.0f);

		private SubViewport _CaptureViewport = null;
		private Camera3D _CaptureCamera = null;
		private TextureRect _DisplayRect = null;

		private void SetupCaptureViewport()
		{
			this._CaptureViewport = new();
			this.AddChild(this._CaptureViewport);

			Vector2I mainViewportSize = Vector2I.Zero;
			Viewport mainViewport = this.Camera.GetViewport();
			if (mainViewport is Window window)
			{
				mainViewportSize = window.Size;
			}
			else if (mainViewport is SubViewport subViewport)
			{
				mainViewportSize = subViewport.Size;
			}

			// Scale the viewport's size according to the render scale
			Vector2I scaledSize = new(
				Mathf.FloorToInt(mainViewportSize.X * this.OutlinesRenderScale),
				Mathf.FloorToInt(mainViewportSize.Y * this.OutlinesRenderScale)
			);

			// Adjust the size so that it's divisible by 8 (in order to work properly with compute shaders)
			this._CaptureViewport.Size = new(
				scaledSize.X % 8 == 0 ? scaledSize.X : scaledSize.X + (8 - scaledSize.X % 8),
				scaledSize.Y % 8 == 0 ? scaledSize.Y : scaledSize.Y + (8 - scaledSize.Y % 8)
			);

			// Disable any performance-impacting feature that would be useless anyway
			this._CaptureViewport.Msaa2D = Viewport.Msaa.Disabled;
			this._CaptureViewport.Msaa3D = Viewport.Msaa.Disabled;
			this._CaptureViewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
			this._CaptureViewport.PositionalShadowAtlasSize = 0;
			this._CaptureViewport.FsrSharpness = 0.0f;

			// We rely on the alpha channel in order to know if a pixel is part of an object to outline
			this._CaptureViewport.TransparentBg = true;
		}

		private void SetupCaptureCamera()
		{
			this._CaptureCamera = new();
			this.AddChild(this._CaptureCamera);

			// Make the main camera not see the outline layer
			this.Camera.CullMask &= ~this.OutlineLayer;
			// Make the capture camera only see the outline layer
			this._CaptureCamera.CullMask = this.OutlineLayer;

			// Add the outlines effect to the camera
			int scaledOutlinesSize = Mathf.CeilToInt(this.OutlinesRenderScale * this.OutlinesSize);
			int scaledGlowRadius = this.GlowRadius == 0 ? 0 : Mathf.CeilToInt(this.OutlinesRenderScale * this.GlowRadius);

			this._CaptureCamera.Compositor = new()
			{
				CompositorEffects =
				[
					new CompositorEffectOutlines(scaledOutlinesSize, scaledGlowRadius)
				]
			};

			this._CaptureCamera.TopLevel = true;
			this._CaptureCamera.MakeCurrent();
		}

		private void SetupDisplayRect()
		{
			this._DisplayRect = new();
			this.AddChild(this._DisplayRect);

			Vector2I mainViewportSize = Vector2I.Zero;
			Viewport mainViewport = this.Camera.GetViewport();

			if (mainViewport is Window window)
			{
				mainViewportSize = window.Size;
			}
			else if (mainViewport is SubViewport subViewport)
			{
				mainViewportSize = subViewport.Size;
			}

			this._DisplayRect.Position = Vector2.Zero;
			this._DisplayRect.Size = mainViewportSize;

			// Bind the viewport's texture to the TextureRect's texture
			this._DisplayRect.Texture = this._CaptureViewport.GetTexture();
			this._DisplayRect.StretchMode = TextureRect.StretchModeEnum.Scale;
			this._DisplayRect.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		}

		public override void _Ready()
		{
			base._Ready();

			this.SetupCaptureViewport();
			this.SetupCaptureCamera();
		}
	}
}
