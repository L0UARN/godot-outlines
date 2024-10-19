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
		[Export(PropertyHint.Range, "0.25,1.0")]
		public float OutlinesRenderScale { get; set; } = 1.0f;
		[Export(PropertyHint.Range, "1,20,1")]
		public int OutlineLayer { get; set; } = 20;

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

			this.OutlinesCaptureViewport.TransparentBg = true;
		}

		private void SetupCaptureCamera()
		{
			for (int i = 1; i < 21; i++)
			{
				if (i == OutlineLayer)
				{
					this.OutlinesCaptureCamera.SetCullMaskValue(i, true);
				}
				else
				{
					this.OutlinesCaptureCamera.SetCullMaskValue(i, false);
				}
			}

			// Add the outlines effect to the camera
			this.OutlinesCaptureCamera.Compositor = new()
			{
				CompositorEffects = new()
				{
					new CompositorEffectOutlines()
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

		public override void _Ready()
		{
			base._Ready();

			SetupCaptureViewport();
			SetupCaptureCamera();
			SetupDisplayRect();
		}

		private void UpdateCaptureCamera()
		{
			Camera3D mainCamera = GetViewport().GetCamera3D();
			if (mainCamera == null)
			{
				return;
			}

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

			UpdateCaptureCamera();
		}
	}
}
