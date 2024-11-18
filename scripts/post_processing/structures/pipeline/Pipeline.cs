using System;
using System.Collections.Generic;
using Godot;
using PostProcessing.Abstractions;
using PostProcessing.Behavior;
using PostProcessing.Structures.Pipeline.Internal;

namespace PostProcessing.Structures.Pipeline
{
	public class Pipeline : ICleanupable
	{
		private RenderingDevice _Rd = null;
		private readonly List<ComputeShader> _Pipeline = new();
		private readonly Dictionary<ComputeShader, PipelineShaderInputOutput> _ShaderInputOutputs = new();

		private ImageBuffer _Buffer1 = null;
		private ImageBuffer _Buffer2 = null;

		private Vector2I _ProcessingSize = Vector2I.MinValue;
		private Vector2I ProcessingSize
		{
			get => this._ProcessingSize;
			set
			{
				if (value.X <= 0 || value.Y <= 0)
				{
					throw new Exception("A pipeline's processing size can't be less than zero.");
				}

				if (this._ProcessingSize.Equals(value))
				{
					return;
				}

				this._Buffer1?.Cleanup();
				this._Buffer1 = new(this._Rd, value);
				this._Buffer2?.Cleanup();
				this._Buffer2 = new(this._Rd, value);

				this._ProcessingSize = value;
			}
		}

		private ImageBuffer _InputImage = null;
		public ImageBuffer InputImage
		{
			get => this._InputImage;
			set
			{
				if (value == null)
				{
					throw new Exception("Can't set the input image of a pipeline to null.");
				}

				if (value.Equals(this._InputImage))
				{
					return;
				}

				// Bind the input image to all shaders that need access to it
				foreach (KeyValuePair<ComputeShader, PipelineShaderInputOutput> shaderInputOutput in this._ShaderInputOutputs)
				{
					if (!shaderInputOutput.Value.HasInputImageAccess)
					{
						shaderInputOutput.Key.BindUniform(value, shaderInputOutput.Value.InputImageSlot);
					}
				}

				// Adjust the processing size to take the new input image into account
				this.ProcessingSize = new(
					Math.Max(value.Size.X, this._OutputImage?.Size.X ?? 0),
					Math.Max(value.Size.Y, this._OutputImage?.Size.Y ?? 0)
				);

				this._InputImage = value;
			}
		}

		private ImageBuffer _OutputImage = null;
		public ImageBuffer OutputImage
		{
			get => this._OutputImage;
			set
			{
				if (value == null)
				{
					throw new Exception("Can't set the output image of a pipeline to null.");
				}

				if (value.Equals(this._OutputImage))
				{
					return;
				}

				// Adjust the processing size to take the new output image into account
				this.ProcessingSize = new(
					Math.Max(value.Size.X, this._InputImage?.Size.X ?? 0),
					Math.Max(value.Size.Y, this._InputImage?.Size.Y ?? 0)
				);

				this._OutputImage = value;
			}
		}

		public Pipeline(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void AddShader(ComputeShader shader, int inputSlot, int outputSlot)
		{
			if (shader == null)
			{
				throw new Exception("Can't add a null step to the pipeline.");
			}

			if (this._Pipeline.Contains(shader))
			{
				throw new Exception("A pipeline can't have two of the same shader. Consider creating a new ComputeShader with the same shader path.");
			}

			// Add the shader as the next step of the pipeline
			this._Pipeline.Add(shader);
			// Register which of its slots are used as input and output
			this._ShaderInputOutputs[shader] = new(inputSlot, outputSlot);
		}

		public void AddShaderWithInputAccess(ComputeShader shader, int inputSlot, int outputSlot, int inputImageSlot)
		{
			this.AddShader(shader, inputSlot, outputSlot);

			if (this._ShaderInputOutputs.TryGetValue(shader, out PipelineShaderInputOutput inputOutput))
			{
				inputOutput.HasInputImageAccess = true;
				inputOutput.InputImageSlot = inputImageSlot;

				if (this._InputImage != null)
				{
					shader.BindUniform(this._InputImage, inputImageSlot);
				}
			}
			else
			{
				throw new Exception("Something went wrong when adding the shader to the pipeline.");
			}
		}

		public void Build()
		{
			if (this._InputImage == null || this._OutputImage == null || this.ProcessingSize.Equals(Vector2I.MinValue))
			{
				throw new Exception("The input and output images must be set before building the pipeline.");
			}

			if (this._Pipeline.Count == 0)
			{
				throw new Exception("The pipeline requires at least one shader in order to be built.");
			}

			for (int i = 0; i < this._Pipeline.Count; i++)
			{
				PipelineShaderInputOutput inputOutput = this._ShaderInputOutputs[this._Pipeline[i]];

				if (this._Pipeline.Count == 1)
				{
					this._Pipeline[i].BindUniform(this._InputImage, inputOutput.InputSlot);
					this._Pipeline[i].BindUniform(this._OutputImage, inputOutput.OutputSlot);
				}
				else if (i == 0)
				{
					this._Pipeline[i].BindUniform(this._InputImage, inputOutput.InputSlot);
					this._Pipeline[i].BindUniform(this._Buffer1, inputOutput.OutputSlot);
				}
				else if (i == this._Pipeline.Count - 1)
				{
					if (i % 2 == 1)
					{
						this._Pipeline[i].BindUniform(this._Buffer1, inputOutput.InputSlot);
						this._Pipeline[i].BindUniform(this._OutputImage, inputOutput.OutputSlot);
					}
					else
					{
						this._Pipeline[i].BindUniform(this._Buffer2, inputOutput.InputSlot);
						this._Pipeline[i].BindUniform(this._OutputImage, inputOutput.OutputSlot);
					}
				}
				else if (i % 2 == 1)
				{
					this._Pipeline[i].BindUniform(this._Buffer1, inputOutput.InputSlot);
					this._Pipeline[i].BindUniform(this._Buffer2, inputOutput.OutputSlot);
				}
				else
				{
					this._Pipeline[i].BindUniform(this._Buffer2, inputOutput.InputSlot);
					this._Pipeline[i].BindUniform(this._Buffer1, inputOutput.OutputSlot);
				}

				if (inputOutput.HasInputImageAccess)
				{
					this._Pipeline[i].BindUniform(this._InputImage, inputOutput.InputImageSlot);
				}
			}

			// TODO: IsBuild member variable
		}

		public void Run()
		{
			for (int i = 0; i < this._Pipeline.Count; i++)
			{
				this._Pipeline[i].Run(this.ProcessingSize);
			}
		}

		public void Cleanup()
		{
			foreach (KeyValuePair<ComputeShader, PipelineShaderInputOutput> shaderInputOutput in this._ShaderInputOutputs)
			{
				shaderInputOutput.Key.UnbindUniform(shaderInputOutput.Value.InputSlot);
				shaderInputOutput.Key.UnbindUniform(shaderInputOutput.Value.OutputSlot);

				if (shaderInputOutput.Value.HasInputImageAccess)
				{
					shaderInputOutput.Key.UnbindUniform(shaderInputOutput.Value.InputImageSlot);
				}
			}

			this._Buffer1?.Cleanup();
			this._Buffer1 = null;
			this._Buffer2?.Cleanup();
			this._Buffer2 = null;
		}
	}
}
