using System;
using System.Collections.Generic;
using Godot;
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
				// TODO: rebuild the graph accounting for the new outlines size
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
				// TODO: rebuild the graph accounting for the new glow radius
			}
		}

		private RenderingDevice _Rd = null;
		private PpcsGraph _Graph = null;
		private readonly List<IPpcsCleanupable> _Resources = new();
		private PpcsShader _FirstShader = null;
		private int _FirstShaderInputSlot = -1;
		private PpcsShader _LastShader = null;
		private int _LastShaderOutputSlot = -1;

		private void SetupEffect()
		{
			this._Rd = RenderingServer.GetRenderingDevice();

			if (this._Rd == null)
			{
				return;
			}

			this._Graph = new(this._Rd);
			this._Resources.Add(this._Graph);

			PpcsShader jfaInit = new(this._Rd, "res://shaders/jfa_init.glsl");
			this._Resources.Add(jfaInit);
			this._FirstShader = jfaInit;
			this._FirstShaderInputSlot = 0;

			int stepsNeeded = Mathf.CeilToInt(Math.Log2(this._OutlinesSize));
			PpcsShader lastJfaStep = null;

			for (int i = stepsNeeded - 1; i >= 0; i--)
			{
				PpcsShader nextJfaStep = new(this._Rd, "res://shaders/jfa_step.glsl");
				this._Resources.Add(nextJfaStep);
				PpcsBuffer stepSizeBuffer = new(this._Rd, BitConverter.GetBytes((int)Mathf.Pow(2, i)));
				this._Resources.Add(stepSizeBuffer);
				nextJfaStep.BindUniform(stepSizeBuffer, 2);

				if (lastJfaStep == null)
				{
					this._Graph.CreateArcFromShaderToShader(jfaInit, 1, nextJfaStep, 0);
				}
				else
				{
					this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, nextJfaStep, 0);
				}

				lastJfaStep = nextJfaStep;
			}

			PpcsShader jfaOutlines = new(this._Rd, "res://shaders/jfa_outlines.glsl");
			this._Resources.Add(jfaOutlines);
			PpcsBuffer outlinesSizeBuffer = new(this._Rd, BitConverter.GetBytes(this._OutlinesSize));
			this._Resources.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 2);
			this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, jfaOutlines, 0);

			this._LastShader = jfaOutlines;
			this._LastShaderOutputSlot = 1;

			if (this._GlowRadius <= 0)
			{
				return;
			}

			PpcsBuffer blurRadiusBuffer = new(this._Rd, BitConverter.GetBytes(this._GlowRadius));
			this._Resources.Add(blurRadiusBuffer);

			PpcsShader boxBlur1 = new(this._Rd, "res://shaders/box_blur.glsl");
			this._Resources.Add(boxBlur1);
			PpcsBuffer blurDirectionBuffer1 = new(this._Rd, BitConverter.GetBytes(true));
			boxBlur1.BindUniform(blurRadiusBuffer, 2);
			boxBlur1.BindUniform(blurDirectionBuffer1, 3);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, boxBlur1, 0);

			PpcsShader boxBlur2 = new(this._Rd, "res://shaders/box_blur.glsl");
			this._Resources.Add(boxBlur2);
			PpcsBuffer blurDirectionBuffer2 = new(this._Rd, BitConverter.GetBytes(false));
			boxBlur2.BindUniform(blurRadiusBuffer, 2);
			boxBlur2.BindUniform(blurDirectionBuffer2, 3);
			this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);

			PpcsShader composite = new(this._Rd, "res://shaders/composite.glsl");
			this._Resources.Add(composite);
			PpcsImage temp = new(this._Rd, new Vector2I(1920, 1080));
			this._Resources.Add(temp);
			composite.BindUniform(temp, 2);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, composite, 0);
			this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);

			this._LastShader = composite;
			this._LastShaderOutputSlot = 2;
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

			// TODO: find a way to make the graph run with multiple input images
			// Maybe just have two inputs images ?

			// for (uint i = 0; i < renderSceneBuffers.GetViewCount(); i++)
			// {
				Rid rawImage = renderSceneBuffers.GetColorLayer(0);

				if (!this._Graph.IsBuilt())
				{
					PpcsImage image = new(this._Rd, rawImage);
					this._Graph.CreateArcFromInputToShader(image, this._FirstShader, this._FirstShaderInputSlot);
					this._Graph.CreateArcFromShaderToOutput(this._LastShader, this._LastShaderOutputSlot, image);
					this._Graph.Build();
				}

				// TODO: resize the graph's buffers to match `rawImage`'s size if needed

				this._Graph.Run();
			// }
		}

		private void Cleanup()
		{
			foreach (IPpcsCleanupable resource in this._Resources)
			{
				resource.Cleanup();
			}

			this._Resources.Clear();
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
