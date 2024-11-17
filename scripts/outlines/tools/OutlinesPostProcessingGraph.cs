using System;
using System.Collections.Generic;
using Godot;
using PostProcessing.Abstractions;
using PostProcessing.Behavior;
using PostProcessing.Structures.Graph;

namespace Outlines.Tools
{
	public class OutlinesPostProcessingGraph : IOutlinesPostProcessing
	{
		private const string SHADER_FOLDER_PATH = "res://assets/outlines/compute_shaders";
		private const string JFA_INIT_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_init.glsl";
		private const string JFA_STEP_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_step.glsl";
		private const string JFA_OUTLINES_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_outlines.glsl";
		private const string BOX_BLUR_SHADER_PATH = $"{SHADER_FOLDER_PATH}/box_blur.glsl";
		private const string COMPOSITE_SHADER_PATH = $"{SHADER_FOLDER_PATH}/composite.glsl";

		private readonly int _OutlinesSize = 4;
		private readonly int _GlowRadius = 2;
		private RenderingDevice _Rd = null;
		private readonly List<ICleanupable> _Resources = new(10);
		private Graph _Graph = null;
		private ImageBuffer _Image = null;

		private void SetupGraph()
		{
			this._Rd = RenderingServer.Singleton.GetRenderingDevice();
			this._Graph = new(this._Rd);

			// First outlines pass
			ComputeShader jfaInit = new(this._Rd, JFA_INIT_SHADER_PATH);
			this._Resources.Add(jfaInit);
			this._Graph.CreateArcFromInputToShader(0, jfaInit, 0);

			// Intermediate outlines passes
			int stepsNeeded = 1 + Mathf.CeilToInt(Math.Log2(this._OutlinesSize));
			ComputeShader lastJfaStep = null;

			for (int i = 0; i < stepsNeeded; i++)
			{
				ComputeShader currentJfaStep = new(this._Rd, JFA_STEP_SHADER_PATH);
				this._Resources.Add(currentJfaStep);

				int jumpDistance = Mathf.FloorToInt(Mathf.Pow(stepsNeeded - i, 2.0f));
				StorageBuffer jumpDistanceBuffer = new(this._Rd, BitConverter.GetBytes(jumpDistance));
				this._Resources.Add(jumpDistanceBuffer);
				currentJfaStep.BindUniform(jumpDistanceBuffer, 2);

				if (lastJfaStep == null)
				{
					this._Graph.CreateArcFromShaderToShader(jfaInit, 1, currentJfaStep, 0);
				}
				else
				{
					this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, currentJfaStep, 0);
				}

				lastJfaStep = currentJfaStep;
			}

			// Final outlines pass
			ComputeShader jfaOutlines = new(this._Rd, JFA_OUTLINES_SHADER_PATH);
			this._Resources.Add(jfaOutlines);
			this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, jfaOutlines, 0);
			this._Graph.CreateArcFromInputToShader(0, jfaOutlines, 1);

			StorageBuffer outlinesSizeBuffer = new(this._Rd, BitConverter.GetBytes(this._OutlinesSize));
			this._Resources.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 3);

			StorageBuffer blurRadiusBuffer = new(this._Rd, BitConverter.GetBytes(this._GlowRadius));
			this._Resources.Add(blurRadiusBuffer);

			// First pass of box blur
			ComputeShader boxBlur1 = new(this._Rd, BOX_BLUR_SHADER_PATH);
			this._Resources.Add(boxBlur1);
			boxBlur1.BindUniform(blurRadiusBuffer, 2);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 2, boxBlur1, 0);

			StorageBuffer directionBuffer1 = new(this._Rd, BitConverter.GetBytes(true));
			this._Resources.Add(directionBuffer1);
			boxBlur1.BindUniform(directionBuffer1, 3);

			// Second pass of box blur
			ComputeShader boxBlur2 = new(this._Rd, BOX_BLUR_SHADER_PATH);
			this._Resources.Add(boxBlur2);
			boxBlur2.BindUniform(blurRadiusBuffer, 2);
			this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);

			StorageBuffer directionBuffer2 = new(this._Rd, BitConverter.GetBytes(false));
			this._Resources.Add(directionBuffer2);
			boxBlur2.BindUniform(directionBuffer2, 3);

			// Combine the outlines pass and the blur pass to create a glow effect
			ComputeShader composite = new(this._Rd, COMPOSITE_SHADER_PATH);
			this._Resources.Add(composite);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 2, composite, 0);
			this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);
			this._Graph.CreateArcFromShaderToOutput(composite, 2, 0);
		}

		public OutlinesPostProcessingGraph(int outlinesSize, int glowRadius)
		{
			this._OutlinesSize = outlinesSize;
			this._GlowRadius = glowRadius;
			this.SetupGraph();
		}

		public void Run(Rid image)
		{
			if (!image.Equals(this._Image?.Rid))
			{
				this._Image = new(this._Rd, image);
				this._Graph.BindInput(0, this._Image);
				this._Graph.BindOutput(0, this._Image);
				this._Graph.ProcessingSize = this._Image.Size;

				if (!this._Graph.IsBuilt())
				{
					this._Graph.Build();
				}
			}

			this._Graph.Run();
		}

		public void Cleanup()
		{
			foreach (ICleanupable resource in this._Resources)
			{
				resource.Cleanup();
			}

			this._Resources.Clear();
		}
	}
}
