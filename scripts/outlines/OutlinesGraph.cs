using System.Collections.Generic;
using Ppcs.Graph;
using Ppcs.Abstractions;
using Ppcs.Interfaces;
using Godot;

namespace Outlines
{
	public class OutlinesGraph : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;
		private readonly List<ICleanupable> _ToCleanup = new();
		private readonly Graph _Graph = null;
		private ImageBuffer _ImageToProcess = null;

		public OutlinesGraph(int outlinesSize, int glowRadius)
		{
			this._Rd = RenderingServer.GetRenderingDevice();

			if (this._Rd == null)
			{
				return;
			}

			this._Graph = new(this._Rd);
			this._ToCleanup.Add(this._Graph);

			ComputeShader jfaInit = new(this._Rd, "res://assets/shaders/jfa_init_debug.glsl");
			this._ToCleanup.Add(jfaInit);
			this._Graph.CreateArcFromInputToShader(0, jfaInit, 0);
			// this._Graph.CreateArcFromShaderToOutput(jfaInit, 1, 0);

			int stepsNeeded = Mathf.CeilToInt(System.Math.Log2(outlinesSize));
			ComputeShader lastJfaStep = null;

			for (int i = stepsNeeded - 1; i >= 0; i--)
			{
				ComputeShader nextJfaStep = new(this._Rd, "res://assets/shaders/jfa_step_debug.glsl");
				this._ToCleanup.Add(nextJfaStep);
				StorageBuffer stepSizeBuffer = new(this._Rd, System.BitConverter.GetBytes((int)Mathf.Pow(2, i)));
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

			// ComputeShader lastShader = lastJfaStep;
			// int lastShaderOutputSlot = 1;

			ComputeShader jfaOutlines = new(this._Rd, "res://assets/shaders/jfa_outlines_debug.glsl");
			this._ToCleanup.Add(jfaOutlines);
			this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, jfaOutlines, 0);
			this._Graph.CreateArcFromInputToShader(0, jfaOutlines, 1);
			StorageBuffer outlinesSizeBuffer = new(this._Rd, System.BitConverter.GetBytes(outlinesSize));
			this._ToCleanup.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 3);

			ComputeShader lastShader = jfaOutlines;
			int lastShaderOutputSlot = 2;

			// if (glowRadius > 0)
			// {
			// 	StorageBuffer blurRadiusBuffer = new(this._Rd, System.BitConverter.GetBytes(glowRadius));
			// 	this._ToCleanup.Add(blurRadiusBuffer);

			// 	ComputeShader boxBlur1 = new(this._Rd, "res://assets/shaders/box_blur.glsl");
			// 	this._ToCleanup.Add(boxBlur1);
			// 	StorageBuffer blurDirectionBuffer1 = new(this._Rd, System.BitConverter.GetBytes(true));
			// 	boxBlur1.BindUniform(blurRadiusBuffer, 2);
			// 	boxBlur1.BindUniform(blurDirectionBuffer1, 3);
			// 	this._Graph.CreateArcFromShaderToShader(jfaOutlines, 2, boxBlur1, 0);

			// 	ComputeShader boxBlur2 = new(this._Rd, "res://assets/shaders/box_blur.glsl");
			// 	this._ToCleanup.Add(boxBlur2);
			// 	StorageBuffer blurDirectionBuffer2 = new(this._Rd, System.BitConverter.GetBytes(false));
			// 	boxBlur2.BindUniform(blurRadiusBuffer, 2);
			// 	boxBlur2.BindUniform(blurDirectionBuffer2, 3);
			// 	this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);

			// 	ComputeShader composite = new(this._Rd, "res://assets/shaders/composite.glsl");
			// 	this._ToCleanup.Add(composite);
			// 	this._Graph.CreateArcFromShaderToShader(jfaOutlines, 2, composite, 0);
			// 	this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);

			// 	lastShader = composite;
			// 	lastShaderOutputSlot = 2;
			// }

			this._Graph.CreateArcFromShaderToOutput(lastShader, lastShaderOutputSlot, 0);
		}

		public void Run(Rid image)
		{
			if (this._Graph == null)
			{
				return;
			}

			if (!image.Equals(this._ImageToProcess?.Rid))
			{
				this._ImageToProcess = new(this._Rd, image);
				this._Graph.ProcessingSize = this._ImageToProcess.Size;
				this._Graph.BindInput(0, this._ImageToProcess);
				this._Graph.BindOutput(0, this._ImageToProcess);

				if (!this._Graph.IsBuilt())
				{
					this._Graph.Build();
				}
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
