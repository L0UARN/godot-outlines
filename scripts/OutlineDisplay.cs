using Godot;

namespace Ppcs
{
	[GlobalClass]
	public partial class OutlineDisplay : Node
	{
		[ExportGroup("Outline Settings")]
		[Export(PropertyHint.Range, "1,32,1")]
		public int OutlineSize { get; set; } = 4;
		[Export(PropertyHint.Range, "1,32,1")]
		public int OutlineLayer { get; set; } = 1;

		[ExportGroup("Required Nodes")]
		[Export]
		public SubViewport CaptureViewport { get; set; } = null;
		[Export]
		public Camera3D CaptureCamera { get; set; } = null;
		[Export]
		public TextureRect OutlineOutput { get; set; } = null;

		private bool SafeToUpdate = false;
		private RenderingDevice RenderingDevice;
		private CsTexture OutlineInputTexture;
		private CsTexture OutlineOutputTexture;
		private CsTexture Blur1OutputTexture;
		private CsTexture Blur2OutputTexture;

		private CsShader OutlineShader;
		private CsUniformTexture OutlineInputTextureUniform;
		private CsUniformTexture OutlineOutputTextureUniform;
		private CsBufferInt OutlineSizeBuffer;
		private CsUniformBuffer OutlineSizeBufferUniform;

		private CsShader BlurShader;
		private CsUniformTexture Blur1InputTextureUniform;
		private CsUniformTexture Blur1OutputTextureUniform;
		private CsUniformTexture Blur2InputTextureUniform;
		private CsUniformTexture Blur2OutputTextureUniform;
		private CsBufferBool BlurAxisBuffer;
		private CsUniformBuffer BlurAxisBufferUniform;
		private CsBufferInt BlurSizeBuffer;
		private CsUniformBuffer BlurSizeBufferUniform;

		private void InitCaptureCamera()
		{
			CaptureCamera.CullMask = (uint)(1 << (OutlineLayer - 1));
		}

		private void InitCaptureViewport()
		{
			Viewport mainViewport = GetViewport();
			Vector2I mainViewportSize = Vector2I.Zero;

			if (mainViewport is Window window)
			{
				mainViewportSize = window.Size;
			}
			else if (mainViewport is SubViewport viewport)
			{
				mainViewportSize = viewport.Size;
			}

			Vector2 computeFriendlySize = new(mainViewportSize.X + (8 - mainViewportSize.X % 8), mainViewportSize.Y + (8 - mainViewportSize.Y % 8));
			CaptureViewport.Size = new((int)computeFriendlySize.X, (int)computeFriendlySize.Y);

			CaptureViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
			CaptureViewport.RenderTargetClearMode = SubViewport.ClearMode.Always;
			CaptureViewport.TransparentBg = true;
		}

		private void InitTextures()
		{
			RenderingDevice = RenderingServer.Singleton.GetRenderingDevice();

			OutlineInputTexture = new(RenderingDevice, new Vector2I(CaptureViewport.Size.X, CaptureViewport.Size.Y));
			OutlineOutputTexture = new(RenderingDevice, new Vector2I(CaptureViewport.Size.X, CaptureViewport.Size.Y));
			Blur1OutputTexture = new(RenderingDevice, new Vector2I(CaptureViewport.Size.X, CaptureViewport.Size.Y));
			Blur2OutputTexture = new(RenderingDevice, new Vector2I(CaptureViewport.Size.X, CaptureViewport.Size.Y));

			Texture2Drd outputTextureObject = new()
			{
				TextureRdRid = Blur2OutputTexture.TextureRid,
			};
			OutlineOutput.Texture = outputTextureObject;
		}

		private void InitOutlineShader()
		{
			OutlineShader = new(RenderingDevice, "res://shaders/outlines_shader.glsl");

			OutlineInputTextureUniform = new(RenderingDevice, OutlineShader.ShaderRid, 0, OutlineInputTexture);
			OutlineShader.BindUniform(OutlineInputTextureUniform);
			OutlineOutputTextureUniform = new(RenderingDevice, OutlineShader.ShaderRid, 1, OutlineOutputTexture);
			OutlineShader.BindUniform(OutlineOutputTextureUniform);

			OutlineSizeBuffer = new(RenderingDevice, OutlineSize);
			OutlineSizeBufferUniform = new(RenderingDevice, OutlineShader.ShaderRid, 3, OutlineSizeBuffer);
			OutlineShader.BindUniform(OutlineSizeBufferUniform);
		}

		private void InitBlurShader()
		{
			BlurShader = new(RenderingDevice, "res://shaders/blur_shader.glsl");

			Blur1InputTextureUniform = new(RenderingDevice, BlurShader.ShaderRid, 0, OutlineOutputTexture);
			Blur1OutputTextureUniform = new(RenderingDevice, BlurShader.ShaderRid, 1, Blur1OutputTexture);
			Blur2InputTextureUniform = new(RenderingDevice, BlurShader.ShaderRid, 0, Blur1OutputTexture);
			Blur2OutputTextureUniform = new(RenderingDevice, BlurShader.ShaderRid, 1, Blur2OutputTexture);

			BlurAxisBuffer = new(RenderingDevice, false);
			BlurAxisBufferUniform = new(RenderingDevice, BlurShader.ShaderRid, 2, BlurAxisBuffer);
			BlurShader.BindUniform(BlurAxisBufferUniform);
			BlurSizeBuffer = new(RenderingDevice, OutlineSize);
			BlurSizeBufferUniform = new(RenderingDevice, BlurShader.ShaderRid, 3, BlurSizeBuffer);
			BlurShader.BindUniform(BlurSizeBufferUniform);
		}

		private void InitOutlines()
		{
			InitTextures();
			InitOutlineShader();
			InitBlurShader();
			SafeToUpdate = true;
		}

		public override void _Ready()
		{
			InitCaptureCamera();
			InitCaptureViewport();
			RenderingServer.Singleton.CallOnRenderThread(Callable.From(InitOutlines));

			GetViewport().SizeChanged += () => {
				InitCaptureViewport();
				RenderingServer.Singleton.CallOnRenderThread(Callable.From(() => {
					Cleanup();
					InitOutlines();
				}));
			};
		}

		private void UpdateCameraSettings()
		{
			Camera3D mainCamera = GetViewport().GetCamera3D();
			CaptureCamera.GlobalTransform = mainCamera.GlobalTransform;
			CaptureCamera.Fov = mainCamera.Fov;
			CaptureCamera.Projection = mainCamera.Projection;
			CaptureCamera.Far = mainCamera.Far;
			CaptureCamera.Near = mainCamera.Near;
		}

		private void UpdateOutlines()
		{
			if (!SafeToUpdate)
			{
				return;
			}

			OutlineSizeBuffer.Data = OutlineSize;
			BlurSizeBuffer.Data = OutlineSize;

			Rid fakeViewportRid = CaptureViewport.GetTexture().GetRid();
			Rid viewportRid = RenderingServer.Singleton.TextureGetRdTexture(fakeViewportRid);
			OutlineInputTexture.Copy(viewportRid, CaptureViewport.Size);
			OutlineShader.Compute(new Vector3I(CaptureViewport.Size.X / 8, CaptureViewport.Size.Y / 8, 1));

			BlurAxisBuffer.Data = false;
			BlurShader.ClearUniforms();
			BlurShader.BindUniform(Blur1InputTextureUniform);
			BlurShader.BindUniform(Blur1OutputTextureUniform);
			BlurShader.BindUniform(BlurAxisBufferUniform);
			BlurShader.BindUniform(BlurSizeBufferUniform);
			BlurShader.Compute(new Vector3I(CaptureViewport.Size.X / 8, CaptureViewport.Size.Y / 8, 1));

			BlurAxisBuffer.Data = true;
			BlurShader.ClearUniforms();
			BlurShader.BindUniform(Blur2InputTextureUniform);
			BlurShader.BindUniform(Blur2OutputTextureUniform);
			BlurShader.BindUniform(BlurAxisBufferUniform);
			BlurShader.BindUniform(BlurSizeBufferUniform);
			BlurShader.Compute(new Vector3I(CaptureViewport.Size.X / 8, CaptureViewport.Size.Y / 8, 1));
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			UpdateCameraSettings();
			RenderingServer.Singleton.CallOnRenderThread(Callable.From(() => UpdateOutlines()));
		}

		private void Cleanup()
		{
			SafeToUpdate = false;

			OutlineInputTexture.Cleanup();
			OutlineOutputTexture.Cleanup();
			Blur1OutputTexture.Cleanup();
			Blur2OutputTexture.Cleanup();
			OutlineShader.Cleanup();
			OutlineInputTextureUniform.Cleanup();
			OutlineOutputTextureUniform.Cleanup();
			OutlineSizeBuffer.Cleanup();
			OutlineSizeBufferUniform.Cleanup();
			BlurShader.Cleanup();
			Blur1InputTextureUniform.Cleanup();
			Blur1OutputTextureUniform.Cleanup();
			Blur2InputTextureUniform.Cleanup();
			Blur2OutputTextureUniform.Cleanup();
			BlurAxisBuffer.Cleanup();
			BlurAxisBufferUniform.Cleanup();
			BlurSizeBuffer.Cleanup();
			BlurSizeBufferUniform.Cleanup();
		}

		public override void _ExitTree()
		{
			base._ExitTree();
			RenderingServer.Singleton.CallOnRenderThread(Callable.From(() => Cleanup()));
		}

		public override void _Notification(int what)
		{
			base._Notification(what);

			if (what == NotificationWMCloseRequest)
			{
				RenderingServer.Singleton.CallOnRenderThread(Callable.From(() => Cleanup()));
			}
		}
	}
}
