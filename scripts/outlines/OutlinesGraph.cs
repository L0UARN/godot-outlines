using System;
using System.Collections.Generic;
using Godot;
using Ppcs.Graph;
using Ppcs.Abstractions;

namespace Outlines
{
	public class OutlinesGraph : ICleanupable
	{
		private readonly Graph _Graph = null;
		private readonly List<ICleanupable> _ToCleanup = new();

		public OutlinesGraph(int outlinesSize, int glowRadius, Rid rawImage)
		{
			RenderingDevice renderingDevice = RenderingServer.GetRenderingDevice();

			if (renderingDevice == null)
			{
				return;
			}

			this._Graph = new(renderingDevice);
			this._ToCleanup.Add(this._Graph);

            Ppcs.Abstractions.Image image = new(renderingDevice, rawImage);

            Ppcs.Abstractions.Shader jfaInit = new(renderingDevice, "res://shaders/jfa_init.glsl");
			this._ToCleanup.Add(jfaInit);
			this._Graph.CreateArcFromInputToShader(image, jfaInit, 0);

			int stepsNeeded = Mathf.CeilToInt(Math.Log2(outlinesSize));
            Ppcs.Abstractions.Shader lastJfaStep = null;

			for (int i = stepsNeeded - 1; i >= 0; i--)
			{
                Ppcs.Abstractions.Shader nextJfaStep = new(renderingDevice, "res://shaders/jfa_step.glsl");
				this._ToCleanup.Add(nextJfaStep);
                Ppcs.Abstractions.Buffer stepSizeBuffer = new(renderingDevice, BitConverter.GetBytes((int)Mathf.Pow(2, i)));
				this._ToCleanup.Add(stepSizeBuffer);
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

            Ppcs.Abstractions.Shader jfaOutlines = new(renderingDevice, "res://shaders/jfa_outlines.glsl");
			this._ToCleanup.Add(jfaOutlines);
            Ppcs.Abstractions.Buffer outlinesSizeBuffer = new(renderingDevice, BitConverter.GetBytes(outlinesSize));
			this._ToCleanup.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 2);
			this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, jfaOutlines, 0);

            Ppcs.Abstractions.Shader lastShader = jfaOutlines;
			int lastShaderOutputSlot = 1;

			if (glowRadius > 0)
			{
                Ppcs.Abstractions.Buffer blurRadiusBuffer = new(renderingDevice, BitConverter.GetBytes(glowRadius));
				this._ToCleanup.Add(blurRadiusBuffer);

                Ppcs.Abstractions.Shader boxBlur1 = new(renderingDevice, "res://shaders/box_blur.glsl");
				this._ToCleanup.Add(boxBlur1);
                Ppcs.Abstractions.Buffer blurDirectionBuffer1 = new(renderingDevice, BitConverter.GetBytes(true));
				boxBlur1.BindUniform(blurRadiusBuffer, 2);
				boxBlur1.BindUniform(blurDirectionBuffer1, 3);
				this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, boxBlur1, 0);

                Ppcs.Abstractions.Shader boxBlur2 = new(renderingDevice, "res://shaders/box_blur.glsl");
				this._ToCleanup.Add(boxBlur2);
                Ppcs.Abstractions.Buffer blurDirectionBuffer2 = new(renderingDevice, BitConverter.GetBytes(false));
				boxBlur2.BindUniform(blurRadiusBuffer, 2);
				boxBlur2.BindUniform(blurDirectionBuffer2, 3);
				this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);

                Ppcs.Abstractions.Shader composite = new(renderingDevice, "res://shaders/composite.glsl");
				this._ToCleanup.Add(composite);
                Ppcs.Abstractions.Image temp = new(renderingDevice, new Vector2I(1920, 1080));
				this._ToCleanup.Add(temp);
				composite.BindUniform(temp, 2);
				this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, composite, 0);
				this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);

				lastShader = composite;
				lastShaderOutputSlot = 2;
			}

			this._Graph.CreateArcFromShaderToOutput(lastShader, lastShaderOutputSlot, image);
			this._Graph.Build();
		}

		public void Run()
		{
			if (this._Graph == null)
			{
				return;
			}

			this._Graph.Run();
		}

		public void Cleanup()
		{
			foreach (ICleanupable cleanupable in this._ToCleanup)
			{
				cleanupable.Cleanup();
			}

			_ToCleanup.Clear();
		}
	}
}
