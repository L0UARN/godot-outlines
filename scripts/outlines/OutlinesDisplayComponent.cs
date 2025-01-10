using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinesDisplayComponent : Node
	{
		private CompositorEffectOutlines _OutlinesEffect = null;
		private SubViewport _OutlinesCaptureViewport = null;
		private Camera3D _OutlinesCaptureCamera = null;
		private TextureRect _DisplayRect = null;
		private bool _SizeChangeHandlerSetup = false;

		private void SetupOutlinesCaptureViewport(Camera3D camera)
		{
			if (camera == null)
			{
				// Cleanup the capture viewport
				this._OutlinesCaptureViewport?.QueueFree();
				this._OutlinesCaptureViewport = null;
				return;
			}

			if (this._OutlinesCaptureViewport == null)
			{
				// Initial setup that should only be done once
				this._OutlinesCaptureViewport = new();
				this.AddChild(this._OutlinesCaptureViewport);

				// Disable any performance-impacting feature that would be useless anyway
				this._OutlinesCaptureViewport.Msaa2D = Viewport.Msaa.Disabled;
				this._OutlinesCaptureViewport.Msaa3D = Viewport.Msaa.Disabled;
				this._OutlinesCaptureViewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
				this._OutlinesCaptureViewport.PositionalShadowAtlasSize = 0;
				this._OutlinesCaptureViewport.FsrSharpness = 0.0f;

				// We rely on the alpha channel in order to know if a pixel is part of an object to outline
				this._OutlinesCaptureViewport.TransparentBg = true;
			}

			Vector2I mainViewportSize = Vector2I.Zero;
			Viewport mainViewport = camera.GetViewport();

			if (mainViewport is Window window)
			{
				mainViewportSize = window.Size;
			}
			else if (mainViewport is SubViewport subViewport)
			{
				mainViewportSize = subViewport.Size;
			}

			// Scale the viewport's size according to the render scale
			this._OutlinesCaptureViewport.Size = new(
				Mathf.FloorToInt(mainViewportSize.X * this.OutlinesRenderScale),
				Mathf.FloorToInt(mainViewportSize.Y * this.OutlinesRenderScale)
			);
		}

		private void SetupOutlinesCaptureCamera(Camera3D camera)
		{
			if (camera == null)
			{
				// Cleanup the capture camera
				this._OutlinesCaptureCamera?.QueueFree();
				this._OutlinesCaptureCamera = null;
				return;
			}

			if (this._OutlinesCaptureCamera == null)
			{
				// Initial setup that should only be done once
				this._OutlinesCaptureCamera = new();
				this._OutlinesCaptureViewport.AddChild(this._OutlinesCaptureCamera);

				// Make the capture camera only see the outline layer
				this._OutlinesCaptureCamera.CullMask = this.OutlineLayer;

				// Add the outlines compositor effect to the capture camera
				this._OutlinesCaptureCamera.Compositor = new()
				{
					CompositorEffects = [this._OutlinesEffect]
				};

				// Make the capture camera top level so it can freely follow the original camera
				this._OutlinesCaptureCamera.TopLevel = true;
				// Make sure the capture camera is active
				this._OutlinesCaptureCamera.MakeCurrent();
			}

			// Make the main camera not see the outline layer
			camera.CullMask &= ~this.OutlineLayer;
		}

		private void SetupDisplayRect(Camera3D camera)
		{
			if (camera == null)
			{
				// Cleanup the display rect
				this._DisplayRect?.QueueFree();
				this._DisplayRect = null;
				return;
			}

			if (this._DisplayRect == null)
			{
				// Initial setup that should only be done once
				this._DisplayRect = new();
				this.AddChild(this._DisplayRect);

				// Bind the viewport's texture to the TextureRect's texture
				this._DisplayRect.Texture = this._OutlinesCaptureViewport.GetTexture();
				this._DisplayRect.StretchMode = TextureRect.StretchModeEnum.Scale;
				this._DisplayRect.TextureFilter = CanvasItem.TextureFilterEnum.Linear;

				// Make it so the user can click through the react, so it doesn't block the UI
				this._DisplayRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			}

			Vector2I mainViewportSize = Vector2I.Zero;
			Viewport mainViewport = camera.GetViewport();

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
		}

		private void HandleSizeChanged()
		{
			this.SetupOutlinesCaptureViewport(this._Camera);
			this.SetupOutlinesCaptureCamera(this._Camera);
			this.SetupDisplayRect(this.Camera);
		}

		private void SetupSizeChangeHandler(Camera3D camera)
		{
			if (this._Camera != null && this._SizeChangeHandlerSetup)
			{
				Viewport oldMainViewport = this._Camera.GetViewport();
				oldMainViewport.SizeChanged -= HandleSizeChanged;
				this._SizeChangeHandlerSetup = false;
			}

			if (camera == null)
			{
				return;
			}

			Viewport mainViewport = camera.GetViewport();
			mainViewport.SizeChanged += HandleSizeChanged;
			this._SizeChangeHandlerSetup = true;
		}

		[Export]
		private Camera3D _Camera = null;
		public Camera3D Camera
		{
			get => this._Camera;
			set
			{
				if (value?.Equals(this._Camera) == true)
				{
					return;
				}

				if (value == null)
				{
					this._Camera = value;
					return;
				}

				this.SetupOutlinesCaptureViewport(value);
				this.SetupOutlinesCaptureCamera(value);
				this.SetupDisplayRect(value);
				this.SetupSizeChangeHandler(value);

				this._Camera = value;
			}
		}

		private void ApplyScaledOutlinesSize(int outlinesSize, float scale)
		{
			float scaled = outlinesSize * scale;

			if (scaled >= 0.0f && scaled <= 1.0f)
			{
				this._OutlinesEffect.OutlinesSize = 1;
				return;
			}

			this._OutlinesEffect.OutlinesSize = Mathf.CeilToInt(scaled);
		}

		private void ApplyScaledGlowRadius(int glowRadius, float scale)
		{
			if (glowRadius == 0)
			{
				this._OutlinesEffect.GlowRadius = glowRadius;
				this._GlowRadius = glowRadius;
				return;
			}

			float scaled = glowRadius * scale;

			if (scaled > 0 && scaled <= 1.0f)
			{
				this._OutlinesEffect.GlowRadius = 1;
				return;
			}

			this._OutlinesEffect.GlowRadius = Mathf.CeilToInt(scaled);
		}

		[ExportCategory("Outlines settings")]
		[Export]
		private int _OutlinesSize = 4;
		public int OutlinesSize
		{
			get => this._OutlinesEffect.OutlinesSize;
			set
			{
				if (value.Equals(this._OutlinesSize))
				{
					return;
				}

				this.ApplyScaledOutlinesSize(value, this._OutlinesRenderScale);
				this._OutlinesSize = value;
			}
		}

		[Export]
		private int _GlowRadius = 2;
		public int GlowRadius
		{
			get => this._OutlinesEffect.GlowRadius;
			set
			{
				if (value.Equals(this._GlowRadius))
				{
					return;
				}

				this.ApplyScaledGlowRadius(value, this._OutlinesRenderScale);
				this._GlowRadius = value;
			}
		}

		[ExportCategory("Technical settings")]
		[Export(PropertyHint.Range, "0.5,1.0,0.05")]
		private float _OutlinesRenderScale = 1.0f;
		public float OutlinesRenderScale
		{
			get => this._OutlinesRenderScale;
			set
			{
				if (value.Equals(this._OutlinesRenderScale))
				{
					return;
				}

				this.ApplyScaledOutlinesSize(this._OutlinesSize, value);
				this.ApplyScaledGlowRadius(this._GlowRadius, value);
				this._OutlinesRenderScale = value;
			}
		}

		[Export(PropertyHint.Layers3DRender)]
		private uint _OutlineLayer = (uint)Mathf.Pow(2.0f, 19.0f);
		public uint OutlineLayer
		{
			get => this._OutlineLayer;
			set
			{
				if (value.Equals(this._OutlineLayer))
				{
					return;
				}

				this._Camera.CullMask &= ~value;
				this._OutlinesCaptureCamera.CullMask = value;

				this._OutlineLayer = value;
			}
		}

		public override void _Ready()
		{
			base._Ready();

			this._OutlinesEffect = new();
			this.ApplyScaledOutlinesSize(this._OutlinesSize, this._OutlinesRenderScale);
			this.ApplyScaledGlowRadius(this._GlowRadius, this._OutlinesRenderScale);

			this.SetupOutlinesCaptureViewport(this._Camera);
			this.SetupOutlinesCaptureCamera(this._Camera);
			this.SetupDisplayRect(this._Camera);
			this.SetupSizeChangeHandler(this._Camera);
		}

		private void UpdateCaptureCamera()
		{
			if (this._Camera == null)
			{
				return;
			}

			// Mimic the main camera
			this._OutlinesCaptureCamera.GlobalTransform = this._Camera.GlobalTransform;
			this._OutlinesCaptureCamera.Projection = this._Camera.Projection;
			this._OutlinesCaptureCamera.Fov = this._Camera.Fov;
			this._OutlinesCaptureCamera.Size = this._Camera.Size;
			this._OutlinesCaptureCamera.Near = this._Camera.Near;
			this._OutlinesCaptureCamera.Far = this._Camera.Far;
			this._OutlinesCaptureCamera.FrustumOffset = this._Camera.FrustumOffset;
			this._OutlinesCaptureCamera.VOffset = this._Camera.VOffset;
			this._OutlinesCaptureCamera.HOffset = this._Camera.HOffset;
			this._OutlinesCaptureCamera.KeepAspect = this._Camera.KeepAspect;
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			this.UpdateCaptureCamera();
		}
	}
}
