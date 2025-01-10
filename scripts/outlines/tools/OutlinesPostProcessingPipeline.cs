using System;
using System.Collections.Generic;
using Godot;
using PostProcessing.Abstractions;
using PostProcessing.Behavior;
using PostProcessing.Structures.Pipeline;

namespace Outlines.Tools
{
	public class OutlinesPostProcessingPipeline : IOutlinesPostProcessing
	{
		private const string SHADER_FOLDER_PATH = "res://assets/outlines/compute_shaders";
		private const string JFA_INIT_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_init.glsl";
		private const string JFA_STEP_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_step.glsl";
		private const string JFA_OUTLINES_SHADER_PATH = $"{SHADER_FOLDER_PATH}/jfa_outlines.glsl";
		private const string COPY_SHADER_PATH = $"{SHADER_FOLDER_PATH}/copy.glsl";

		private readonly int _OutlinesSize = 4;
		private RenderingDevice _Rd = null;
		private readonly List<ICleanupable> _Resources = new(6);
		private Pipeline _Pipeline = null;
		private ImageBuffer _Image = null;

		private void SetupPipeline()
		{
			this._Rd = RenderingServer.Singleton.GetRenderingDevice();
			this._Pipeline = new(this._Rd);

			ComputeShader jfaInit = new(this._Rd, JFA_INIT_SHADER_PATH);
			this._Resources.Add(jfaInit);
			this._Pipeline.AddShader(jfaInit, 0, 1);

			int stepsNeeded = 1 + Mathf.CeilToInt(Math.Log2(this._OutlinesSize));

			for (int i = 0; i < stepsNeeded; i++)
			{
				ComputeShader jfaStep = new(this._Rd, JFA_STEP_SHADER_PATH);
				this._Resources.Add(jfaStep);

				int jumpDistance = Mathf.FloorToInt(Mathf.Pow(stepsNeeded - i, 2.0f));
				StorageBuffer jumpDistanceBuffer = new(this._Rd, BitConverter.GetBytes(jumpDistance));
				this._Resources.Add(jumpDistanceBuffer);
				jfaStep.BindUniform(jumpDistanceBuffer, 2);

				this._Pipeline.AddShader(jfaStep, 0, 1);
			}

			ComputeShader jfaOutlines = new(this._Rd, JFA_OUTLINES_SHADER_PATH);
			this._Resources.Add(jfaOutlines);

			StorageBuffer outlinesSizeBuffer = new(this._Rd, BitConverter.GetBytes(this._OutlinesSize));
			this._Resources.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 3);

			this._Pipeline.AddShaderWithInputAccess(jfaOutlines, 0, 2, 1);

			// This step may seem unecessary, but it prevents weird glitches from happening:
			// - jfaOutlines is reading from and writing to the output image at the same time
			// - this creates some artifacts on the output image
			// - to fix it, we add another step in between jfaOutlines and the output
			// - this way, jfaOutlines instead writes to a buffer image as it reads from the output image
			ComputeShader copy = new(this._Rd, COPY_SHADER_PATH);
			this._Resources.Add(copy);
			this._Pipeline.AddShader(copy, 0, 1);
		}

		public OutlinesPostProcessingPipeline(int outlinesSize)
		{
			this._OutlinesSize = outlinesSize;
			this.SetupPipeline();
		}

		public void Run(Rid outlinesColor, Rid outlinesDepth, Rid mainDepth)
		{
			if (!outlinesColor.Equals(this._Image?.Rid))
			{
				this._Image = new(this._Rd, outlinesColor);
				this._Pipeline.InputImage = this._Image;
				this._Pipeline.OutputImage = this._Image;

				if (!this._Pipeline.IsBuilt)
				{
					this._Pipeline.Build();
				}
			}

			this._Pipeline.Run();
		}

		public void Cleanup()
		{
			foreach (ICleanupable resource in this._Resources)
			{
				resource.Cleanup();
			}

			this._Resources.Clear();
			this._Pipeline.Cleanup();
		}
	}
}
