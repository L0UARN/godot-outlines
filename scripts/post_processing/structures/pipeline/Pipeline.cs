using System;
using System.Collections.Generic;
using Godot;
using PostProcessing.Abstractions;
using PostProcessing.Behavior;

namespace PostProcessing.Structures.Pipeline
{
	public class Pipeline : ICleanupable
	{
		private RenderingDevice _Rd = null;
		private readonly List<ComputeShader> _Pipeline = new();
		private readonly Dictionary<ComputeShader, int> _ShaderInputs = new();
		private readonly Dictionary<ComputeShader, int> _ShaderOutputs = new();
		private readonly Dictionary<ComputeShader, int> _InputAccesses = new();

		private ImageBuffer _Buffer1 = null;
		private ImageBuffer _Buffer2 = null;

		private Vector2I _ProcessingSize = Vector2I.MinValue;
		public Vector2I ProcessingSize
		{
			get => this._ProcessingSize;
			set
			{
				if (this._ProcessingSize.Equals(value))
				{
					return;
				}

				if (value.X <= 0 || value.Y <= 0)
				{
					throw new Exception("A pipeline's processing size can't be less than zero.");
				}

				this._Buffer1?.Cleanup();
				this._Buffer1 = new(this._Rd, value);
				this._Buffer2?.Cleanup();
				this._Buffer2 = new(this._Rd, value);

				this._ProcessingSize = value;
			}
		}

		public Pipeline(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void AddStep(ComputeShader step, int inputSlot, int outputSlot)
		{
			if (step == null)
			{
				throw new Exception("Can't add a null step to the pipeline.");
			}

			if (this._Pipeline.Contains(step))
			{
				throw new Exception("A pipeline can't have two of the same shader. Consider creating a new ComputeShader with the same shader path.");
			}

			this._Pipeline.Add(step);
			this._ShaderInputs[step] = inputSlot;
			this._ShaderOutputs[step] = outputSlot;
		}

		public void GiveInputAccessToStep(ComputeShader step, int slot)
		{
			if (!this._Pipeline.Contains(step))
			{
				throw new Exception("Can't give access to the input image to a step that doens't exist.");
			}

			this._InputAccesses[step] = slot;

			// TODO: bind the input image to the shader
		}

		public void Run()
		{

		}

		public void Cleanup()
		{
			foreach (KeyValuePair<ComputeShader, int> inputAccess in this._InputAccesses)
			{
				inputAccess.Key.UnbindUniform(inputAccess.Value);
			}

			this._Buffer1?.Cleanup();
			this._Buffer1 = null;
			this._Buffer2?.Cleanup();
			this._Buffer2 = null;
		}
	}
}
