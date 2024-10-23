using System;
using Godot;
using Outlines.Ppcs;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect
	{
		private void SetupStepsNeeded()
		{
			this._StepsNeeded = Mathf.CeilToInt(Math.Log2(this._OutlinesSize));
		}

		private int _StepsNeeded = -1;
		private int _OutlinesSize = 16;
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

				this._OutlinesSize = value;
				this.SetupStepsNeeded();
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
			this._Pipeline.Steps.Add(jfaInit);

			for (int i = this._StepsNeeded - 1; i >= 0; i--)
			{
				PpcsShader jfaStep = new(this._Rd, "res://shaders/jfa_step.glsl");
				PpcsBuffer stepSizeBuffer = new(this._Rd, BitConverter.GetBytes((int)Mathf.Pow(2, i)));
				PpcsUniformBuffer stepSizeBufferUniform = new(this._Rd, jfaStep, 2, stepSizeBuffer);
				jfaStep.Uniforms.Add(stepSizeBufferUniform);
				this._Pipeline.Steps.Add(jfaStep);
			}

			PpcsShader jfaOutlines = new(this._Rd, "res://shaders/jfa_outlines.glsl");
			PpcsBuffer outlinesSizeBuffer = new(this._Rd, BitConverter.GetBytes(this._OutlinesSize));
			PpcsUniformBuffer outlinesSizeBufferUniform = new(this._Rd, jfaOutlines, 2, outlinesSizeBuffer);
			jfaOutlines.Uniforms.Add(outlinesSizeBufferUniform);
			this._Pipeline.Steps.Add(jfaOutlines);
		}

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			this.SetupStepsNeeded();

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

		private void Cleanup()
		{
			foreach (PpcsShader shader in this._Pipeline.Steps)
			{
				foreach (PpcsUniform uniform in shader.Uniforms)
				{
					// The uniforms must be cleaned up before the objects
					uniform.Cleanup();

					if (uniform is PpcsUniformBuffer bufferUniform)
					{
						bufferUniform.Buffer.Cleanup();
					}
					else if (uniform is PpcsUniformImage imageUniform)
					{
						imageUniform.Image.Cleanup();
					}
				}

				// The shaders must be cleaned up after the uniforms
				shader.Cleanup();
			}

			this._Pipeline.Cleanup();
		}

		public override void _Notification(int what)
		{
			base._Notification(what);

			if (what != NotificationPredelete)
			{
				return;
			}

			this.Cleanup();
		}
	}
}
