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
		private readonly RenderingDevice _Rd = null;

		private readonly List<ComputeShader> _Pipeline = [];
		private readonly Dictionary<ComputeShader, PipelineShaderInputOutput> _ShaderInputOutputs = [];
		public bool IsBuilt { get; private set; } = false;

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

				// Create new buffers of the new processing size
				ImageBuffer newBuffer1 = new(this._Rd, value);
				ImageBuffer newBuffer2 = new(this._Rd, value);

				// If the pipeline has already been built, the buffers need to be resized
				if (this.IsBuilt)
				{
					// For each shader in the pipeline, re-bind its input and outputs to the new buffers
					for (int i = 0; i < this._Pipeline.Count; i++)
					{
						if (!this._ShaderInputOutputs.TryGetValue(this._Pipeline[i], out PipelineShaderInputOutput inputOutput))
						{
							throw new Exception("One of the pipeline's shader has not been correctly added.");
						}

						if (i == 0)
						{
							this._Pipeline[i].BindUniform(newBuffer1, inputOutput.OutputSlot);
						}
						else if (i == this._Pipeline.Count - 1)
						{
							if (i % 2 == 1)
							{
								this._Pipeline[i].BindUniform(newBuffer1, inputOutput.InputSlot);
							}
							else
							{
								this._Pipeline[i].BindUniform(newBuffer2, inputOutput.InputSlot);
							}
						}
						else if (i % 2 == 1)
						{
							this._Pipeline[i].BindUniform(newBuffer1, inputOutput.InputSlot);
							this._Pipeline[i].BindUniform(newBuffer2, inputOutput.OutputSlot);
						}
						else
						{
							this._Pipeline[i].BindUniform(newBuffer2, inputOutput.InputSlot);
							this._Pipeline[i].BindUniform(newBuffer1, inputOutput.OutputSlot);
						}
					}
				}

				// Cleanup the former buffers
				this._Buffer1?.Cleanup();
				this._Buffer1 = newBuffer1;
				this._Buffer2?.Cleanup();
				this._Buffer2 = newBuffer2;

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

				// If the pipeline has already been built
				if (this.IsBuilt)
				{
					// Re-bind the new input image to all shaders that need access to it
					foreach (KeyValuePair<ComputeShader, PipelineShaderInputOutput> shaderInputOutput in this._ShaderInputOutputs)
					{
						if (shaderInputOutput.Key.Equals(this._Pipeline[0]))
						{
							shaderInputOutput.Key.BindUniform(value, shaderInputOutput.Value.InputSlot);
						}

						if (shaderInputOutput.Value.HasInputImageAccess)
						{
							shaderInputOutput.Key.BindUniform(value, shaderInputOutput.Value.InputImageSlot);
						}
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

				// If the pipeline has already been built
				if (this.IsBuilt)
				{
					// Re-bind the new output image to the output of the last shader in the pipeline
					ComputeShader last = this._Pipeline[^1];
					PipelineShaderInputOutput inputOutput = this._ShaderInputOutputs[last];
					last.BindUniform(value, inputOutput.OutputSlot);
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
			if (renderingDevice == null)
			{
				throw new Exception("The pipeline needs a non-null rendering device in order to work.");
			}

			this._Rd = renderingDevice;
		}

		public void AddShader(ComputeShader shader, int inputSlot, int outputSlot)
		{
			if (this.IsBuilt)
			{
				throw new Exception("Can't add a shader to the pipeline once it has been built.");
			}

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
			}
			else
			{
				throw new Exception("Something went wrong when adding the shader to the pipeline.");
			}
		}

		public void Build()
		{
			if (this.IsBuilt)
			{
				throw new Exception("The pipeline has already been built.");
			}

			if (this._InputImage == null || this._OutputImage == null || this.ProcessingSize.Equals(Vector2I.MinValue))
			{
				throw new Exception("The input and output images must be set before building the pipeline.");
			}

			if (this._Pipeline.Count == 0)
			{
				throw new Exception("The pipeline requires at least one shader in order to be built.");
			}

			// For each shader in the pipeline, bind its input and outputs:
			// - to the input image and first buffer if it's the first shader in the pipeline
			// - to the last buffer used and the output image if it's the last shader in the pipeline
			// - to the first and second buffer, alternating based of the position in the pipeline of the shader if it's not first or last
			// Also bind the input image to it if the shader requires it

			for (int i = 0; i < this._Pipeline.Count; i++)
			{
				if (!this._ShaderInputOutputs.TryGetValue(this._Pipeline[i], out PipelineShaderInputOutput inputOutput))
				{
					throw new Exception("One of the pipeline's shader has not been correctly added.");
				}

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

			this.IsBuilt = true;
		}

		public void Run()
		{
			if (!this.IsBuilt)
			{
				throw new Exception("Can't run the pipeline before it has been built.");
			}

			for (int i = 0; i < this._Pipeline.Count; i++)
			{
				this._Pipeline[i].Run(this.ProcessingSize);
			}
		}

		public void Cleanup()
		{
			this.IsBuilt = false;

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
