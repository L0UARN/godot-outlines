using System;
using System.ComponentModel;
using Godot;
using Outlines.Ppcs;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect
	{
		private int _StepsNeeded = 2;
		private int _OutlinesSize = 4;
		[Export]
		public int OutlinesSize
		{
			get => this._OutlinesSize;
			set
			{
				if (Engine.IsEditorHint())
				{
					this._OutlinesSize = value;
					return;
				}

				if (value == this._OutlinesSize)
				{
					return;
				}

				this._StepsNeeded = Mathf.CeilToInt(Math.Log2(value));
				this._OutlinesSize = value;
			}
		}

		private RenderingDevice _Rd = null;
		private PpcsPipeline _Pipeline = null;

		private void SetupEffect()
		{
			this._Rd = RenderingServer.GetRenderingDevice();
			if (this._Rd == null)
			{
				return;
			}

			this._Pipeline = new(this._Rd);

			PpcsShader jfaInit = new(this._Rd, "res://shaders/jfa_init.glsl");
			this._Pipeline.AddStep(jfaInit);

			// TODO: find a way to make that more cleanup-able
			for (int i = 0; i < this._StepsNeeded; i++)
			{
				PpcsShader jfaStep = new(this._Rd, "res://shaders/jfa_step.glsl");
				PpcsBuffer stepSizeBuffer = new(this._Rd, BitConverter.GetBytes(i));
				PpcsUniformBuffer stepSizeBufferUniform = new(this._Rd, jfaStep, 2, stepSizeBuffer);
				jfaStep.BindUniform(stepSizeBufferUniform);
				this._Pipeline.AddStep(jfaStep);
			}
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
