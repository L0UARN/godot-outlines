using System;
using Godot;
using Outlines.Ppcs;
using Outlines.Ppcs.Structs;
using Outlines.Ppcs.Utils;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect
	{
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

				this._OutlinesSize = value;
				// TODO: rebuild the pipeline accounting for the new outlines size
			}
		}

		private int _GlowRadius = 2;
		[Export(PropertyHint.Range, "0,32,1")]
		public int GlowRadius
		{
			get => this._GlowRadius;
			set
			{
				if (Engine.IsEditorHint())
				{
					this._GlowRadius = value;
					return;
				}

				if (value == this._GlowRadius)
				{
					return;
				}

				this._GlowRadius = value;
				// TODO: rebuild the pipeline accounting for the new glow radius
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

			int stepsNeeded = Mathf.CeilToInt(Math.Log2(this._OutlinesSize));
			for (int i = stepsNeeded - 1; i >= 0; i--)
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

			// if (this._GlowRadius > 0)
			// {
			// 	PpcsShader glowPass1 = new(this._Rd, "res://shaders/glow.glsl");
			// 	PpcsBuffer glowPass1SizeBuffer = new(this._Rd, BitConverter.GetBytes(this._GlowRadius));
			// 	PpcsUniformBuffer glowPass1SizeBufferUniform = new(this._Rd, glowPass1, 2, glowPass1SizeBuffer);
			// 	glowPass1.Uniforms.Add(glowPass1SizeBufferUniform);
			// 	PpcsBuffer glowPass1DirectionBuffer = new(this._Rd, BitConverter.GetBytes(false));
			// 	PpcsUniformBuffer glowPass1DirectionBufferUniform = new(this._Rd, glowPass1, 3, glowPass1DirectionBuffer);
			// 	glowPass1.Uniforms.Add(glowPass1DirectionBufferUniform);
			// 	this._Pipeline.Steps.Add(glowPass1);

			// 	PpcsShader glowPass2 = new(this._Rd, "res://shaders/glow.glsl");
			// 	PpcsBuffer glowPass2SizeBuffer = new(this._Rd, BitConverter.GetBytes(this._GlowRadius));
			// 	PpcsUniformBuffer glowPass2SizeBufferUniform = new(this._Rd, glowPass2, 2, glowPass2SizeBuffer);
			// 	glowPass2.Uniforms.Add(glowPass2SizeBufferUniform);
			// 	PpcsBuffer glowPass2DirectionBuffer = new(this._Rd, BitConverter.GetBytes(true));
			// 	PpcsUniformBuffer glowPass2DirectionBufferUniform = new(this._Rd, glowPass2, 3, glowPass2DirectionBuffer);
			// 	glowPass2.Uniforms.Add(glowPass2DirectionBufferUniform);
			// 	this._Pipeline.Steps.Add(glowPass2);
			// }
		}

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			RenderingServer.CallOnRenderThread(Callable.From(this.SetupEffect));
		}

		public CompositorEffectOutlines(int outlinesSize, int glowRadius) : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			this._OutlinesSize = outlinesSize;
			this._GlowRadius = glowRadius;
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
