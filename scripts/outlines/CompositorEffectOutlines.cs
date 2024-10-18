using Godot;
using Outlines.Ppcs;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect
	{
		private RenderingDevice _Rd = null;
		private PpcsPipeline _Pipeline = null;

		private void SetupEffect()
		{
			this._Rd = RenderingServer.GetRenderingDevice();
			if (this._Rd == null)
			{
				return;
			}

			PpcsShader colorInversionShader = new(this._Rd, "res://shaders/test.glsl");
			PpcsShader darkeningShader = new(this._Rd, "res://shaders/test2.glsl");
			PpcsShader redeningShader = new(this._Rd, "res://shaders/test3.glsl");

			this._Pipeline = new(this._Rd);
			this._Pipeline.AddStep(colorInversionShader);
			this._Pipeline.AddStep(darkeningShader);
			this._Pipeline.AddStep(redeningShader);
		}

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			RenderingServer.CallOnRenderThread(Callable.From(this.SetupEffect));
		}

		public override void _RenderCallback(int effectCallbackType, RenderData renderData)
		{
			base._RenderCallback(effectCallbackType, renderData);

			if (this._Rd == null)
			{
				return;
			}

			RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();
			for (uint i = 0; i < renderSceneBuffers.GetViewCount(); i++)
			{
				Rid rawImage = renderSceneBuffers.GetColorLayer(i);
				PpcsImage image = new(this._Rd, rawImage);
				this._Pipeline.Run(image, image);
			}
		}

		public override void _Notification(int what)
		{
			base._Notification(what);

			if (what == NotificationPredelete)
			{
				this._Pipeline.Cleanup();
			}
		}
	}
}
