using System;
using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinesDisplayComponent : Node
	{
		[ExportCategory("Required nodes")]
		[Export]
		public SubViewport OutlinesCaptureViewport { get; set; } = null;
		[Export]
		public Camera3D OutlinesCaptureCamera { get; set; } = null;
		[Export]
		public TextureRect OutlinesDisplayRect { get; set; } = null;

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

			// Scale the viewport's size according to the render scale
			Vector2I scaledSize = new(
				Mathf.FloorToInt(mainViewportSize.X * this.OutlinesRenderScale),
				Mathf.FloorToInt(mainViewportSize.Y * this.OutlinesRenderScale)
			);

			// Adjust the size so that it's divisible by 8 (in order to work properly with compute shaders)
			this.OutlinesCaptureViewport.Size = new(
				scaledSize.X % 8 == 0 ? scaledSize.X : scaledSize.X + (8 - scaledSize.X % 8),
				scaledSize.Y % 8 == 0 ? scaledSize.Y : scaledSize.Y + (8 - scaledSize.Y % 8)
			);

			// Disable any performance-impacting feature that would be useless anyway
			this.OutlinesCaptureViewport.Msaa2D = Viewport.Msaa.Disabled;
			this.OutlinesCaptureViewport.Msaa3D = Viewport.Msaa.Disabled;
			this.OutlinesCaptureViewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
			this.OutlinesCaptureViewport.PositionalShadowAtlasSize = 0;
			this.OutlinesCaptureViewport.FsrSharpness = 0.0f;

			// Very important so the outlines layer is to be applied on top of the existing layer and does not obscure it
			this.OutlinesCaptureViewport.TransparentBg = true;
		}

		private void SetupCaptureCamera()
		{
			Camera3D mainCamera = this.GetViewport().GetCamera3D();

			if (mainCamera == null)
			{
				return;
			}

			// Make the main camera not see the outline layer
			mainCamera.CullMask &= ~this.OutlineLayer;
			// Make the capture camera only see the outline layer
			this.OutlinesCaptureCamera.CullMask = this.OutlineLayer;

			// Add the outlines effect to the camera
			int scaledOutlinesSize = Mathf.CeilToInt(this.OutlinesRenderScale * this.OutlinesSize);
			int scaledGlowRadius = this.GlowRadius == 0 ? 0 : Mathf.CeilToInt(this.OutlinesRenderScale * this.GlowRadius);

			this.OutlinesCaptureCamera.Compositor = new()
			{
				CompositorEffects = new()
				{
					new CompositorEffectOutlines(scaledOutlinesSize, scaledGlowRadius)
				}
			};

			this.OutlinesCaptureCamera.TopLevel = true;
			this.OutlinesCaptureCamera.MakeCurrent();
		}

		private void SetupDisplayRect()
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

			this.OutlinesDisplayRect.Position = Vector2.Zero;
			this.OutlinesDisplayRect.Size = mainViewportSize;

			// Bind the viewport's texture to the TextureRect's texture
			this.OutlinesDisplayRect.Texture = this.OutlinesCaptureViewport.GetTexture();
			this.OutlinesDisplayRect.StretchMode = TextureRect.StretchModeEnum.Scale;
		}

		private void HandleSizeChanged()
		{
			this.SetupCaptureViewport();
			this.SetupDisplayRect();
		}

		public override void _Ready()
		{
			base._Ready();

			this.SetupCaptureViewport();
			this.SetupCaptureCamera();
			this.SetupDisplayRect();

			Viewport mainViewport = this.GetViewport();
			mainViewport.SizeChanged += this.HandleSizeChanged;
		}

		private void UpdateCaptureCamera()
		{
			Camera3D mainCamera = this.GetViewport().GetCamera3D();

			if (mainCamera == null)
			{
				return;
			}

			// Mimic the main camera
			this.OutlinesCaptureCamera.GlobalTransform = mainCamera.GlobalTransform;
			this.OutlinesCaptureCamera.Projection = mainCamera.Projection;
			this.OutlinesCaptureCamera.Fov = mainCamera.Fov;
			this.OutlinesCaptureCamera.Size = mainCamera.Size;
			this.OutlinesCaptureCamera.Near = mainCamera.Near;
			this.OutlinesCaptureCamera.Far = mainCamera.Far;
			this.OutlinesCaptureCamera.FrustumOffset = mainCamera.FrustumOffset;
			this.OutlinesCaptureCamera.VOffset = mainCamera.VOffset;
			this.OutlinesCaptureCamera.HOffset = mainCamera.HOffset;
			this.OutlinesCaptureCamera.KeepAspect = mainCamera.KeepAspect;
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			this.UpdateCaptureCamera();
		}
	}
}
