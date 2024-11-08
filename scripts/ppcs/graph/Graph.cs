using System;
using System.Collections.Generic;
using Godot;
using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class Graph : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private readonly Dictionary<int, Abstractions.Image> _Inputs = new();
		private readonly Dictionary<int, HashSet<GraphArcFromInputToShader>> _InputGraph = new();
		private readonly Dictionary<int, Abstractions.Image> _Outputs = new();
		private readonly Dictionary<int, HashSet<GraphArcFromShaderToOutput>> _OutputGraph = new();
		private readonly Dictionary<Abstractions.Shader, HashSet<GraphArcFromShaderToShader>> _ShaderGraph = new();

		private Vector2I _ProcessingSize = Vector2I.Zero;
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
					throw new Exception("A graph's processing size can't be less than zero.");
				}

				if (!this.IsBuilt())
				{
					this._ProcessingSize = value;
					return;
				}

				foreach (KeyValuePair<Abstractions.Image, HashSet<GraphBufferBinding>> buffer in this._BufferBindings)
				{
					foreach (GraphBufferBinding binding in buffer.Value)
					{
						binding.Shader.UnbindUniform(binding.Slot);
					}

					buffer.Key.Size = value;

					foreach (GraphBufferBinding binding in buffer.Value)
					{
						binding.Shader.BindUniform(buffer.Key, binding.Slot);
					}
				}

				this._ProcessingSize = value;
			}
		}

		private readonly Dictionary<Abstractions.Image, HashSet<GraphBufferBinding>> _BufferBindings = new();
		private readonly List<Abstractions.Shader> _Pipeline = new();

		public Graph(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void CreateArcFromInputToShader(int fromInput, Abstractions.Shader toShader, int toShaderSlot)
		{
			if (!this._InputGraph.ContainsKey(fromInput))
			{
				this._InputGraph[fromInput] = new(1);
			}

			this._InputGraph[fromInput].Add(new(toShader, toShaderSlot));

			// Bind the image if it's already known
			if (this._Inputs.ContainsKey(fromInput))
			{
				toShader.BindUniform(this._Inputs[fromInput], toShaderSlot);
			}
		}

		public void BindInput(int input, Abstractions.Image inputImage)
		{
			if (this._Inputs.TryGetValue(input, out Abstractions.Image previous))
			{
				if (previous.Equals(inputImage))
				{
					return;
				}
			}

			this._Inputs[input] = inputImage;

			// Bind the new input image to all the shaders that require it
			if (this._InputGraph.TryGetValue(input, out HashSet<GraphArcFromInputToShader> arcs))
			{
				foreach (GraphArcFromInputToShader arc in arcs)
				{
					arc.ToShader.BindUniform(inputImage, arc.ToShaderSlot);
				}
			}
		}

		public void CreateArcFromShaderToShader(Abstractions.Shader fromShader, int fromShaderSlot, Abstractions.Shader toShader, int toShaderSlot)
		{
			if (!this._ShaderGraph.ContainsKey(fromShader))
			{
				this._ShaderGraph[fromShader] = new(1);
			}

			GraphArcFromShaderToShader newArc = new(fromShaderSlot, toShader, toShaderSlot);
			this._ShaderGraph[fromShader].Add(newArc);

			// Check if creating the arc has created a cycle in the graph
			Stack<Abstractions.Shader> toVisit = new();
			toVisit.Push(fromShader);

			while (toVisit.Count > 0)
			{
				Abstractions.Shader justVisited = toVisit.Pop();

				if (!this._ShaderGraph.ContainsKey(justVisited))
				{
					continue;
				}

				foreach (GraphArcFromShaderToShader arc in this._ShaderGraph[justVisited])
				{
					if (arc.ToShader.Equals(fromShader))
					{
						this._ShaderGraph[fromShader].Remove(newArc);
						throw new Exception("Creating this arc would create a cycle in the graph.");
					}

					toVisit.Push(arc.ToShader);
				}
			}
		}

		public void CreateArcFromShaderToOutput(Abstractions.Shader fromShader, int fromShaderSlot, int toOutput)
		{
			if (!this._OutputGraph.ContainsKey(toOutput))
			{
				this._OutputGraph[toOutput] = new(1);
			}

			this._OutputGraph[toOutput].Add(new(fromShader, fromShaderSlot));

			// Bind the image if it's already known
			if (this._Outputs.ContainsKey(toOutput))
			{
				fromShader.BindUniform(this._Outputs[toOutput], fromShaderSlot);
			}
		}

		public void BindOutput(int output, Abstractions.Image outputImage)
		{
			if (this._Outputs.TryGetValue(output, out Abstractions.Image previous))
			{
				if (previous.Equals(outputImage))
				{
					return;
				}
			}

			this._Outputs[output] = outputImage;

			// Bind the new output image to all the shaders that require it
			if (this._OutputGraph.TryGetValue(output, out HashSet<GraphArcFromShaderToOutput> arcs))
			{
				foreach (GraphArcFromShaderToOutput arc in arcs)
				{
					arc.FromShader.BindUniform(outputImage, arc.FromShaderSlot);
				}
			}
		}

		public void Build()
		{
			if (this._ProcessingSize.Equals(Vector2I.Zero))
			{
				throw new Exception("Can't build the graph before having set the processing size.");
			}

			// Start visiting the shader graph starting with the ones that use an input image
			Queue<Abstractions.Shader> toVisit = new();

			foreach (KeyValuePair<int, HashSet<GraphArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (GraphArcFromInputToShader arcDestination in inputArc.Value)
				{
					toVisit.Enqueue(arcDestination.ToShader);
				}
			}

			// Store the buffers that are written by a shader, so that multiple shaders can read from the same output without creating multiple buffers
			Dictionary<GraphBufferBinding, Abstractions.Image> outputBuffers = new();

			while (toVisit.Count > 0)
			{
				Abstractions.Shader justVisited = toVisit.Dequeue();
				int pipelineInsertIndex = -1;

				// Explore the shaders that depend on `justVisited`
				if (this._ShaderGraph.ContainsKey(justVisited))
				{
					foreach (GraphArcFromShaderToShader shaderArc in this._ShaderGraph[justVisited])
					{
						int toShaderIndex = this._Pipeline.IndexOf(shaderArc.ToShader);

						// No need to visit a shader that has already been visited
						if (toShaderIndex == -1)
						{
							toVisit.Enqueue(shaderArc.ToShader);
						}

						// If the shader that depends on `justVisited` is before `justVisited` in the pipeline, then make it so `justVisited` is inserted before it
						if (pipelineInsertIndex == -1 || (toShaderIndex != -1 && toShaderIndex < pipelineInsertIndex))
						{
							pipelineInsertIndex = toShaderIndex;
						}

						// Find out if creating a new buffer is needed, or if there's already one for this output
						Abstractions.Image buffer = null;
						GraphBufferBinding bufferBinding = new(justVisited, shaderArc.FromShaderSlot);

						if (outputBuffers.ContainsKey(bufferBinding))
						{
							buffer = outputBuffers[bufferBinding];
						}
						else
						{
							buffer = new(this._Rd, this._ProcessingSize);
							outputBuffers[bufferBinding] = buffer;
						}

						if (!this._BufferBindings.ContainsKey(buffer))
						{
							this._BufferBindings[buffer] = new(2);
						}

						// Bind the output of `justVisited` to the input of the shader that depends on it
						justVisited.BindUniform(buffer, shaderArc.FromShaderSlot);
						this._BufferBindings[buffer].Add(new(justVisited, shaderArc.FromShaderSlot));
						// Bind the input of the shader that depends on `justVisited`
						shaderArc.ToShader.BindUniform(buffer, shaderArc.ToShaderSlot);
						this._BufferBindings[buffer].Add(new(shaderArc.ToShader, shaderArc.ToShaderSlot));
					}
				}

				// Add `justVisited` at the end of the pipeline if no shaders that depends on it are already in the pipeline
				if (pipelineInsertIndex == -1)
				{
					this._Pipeline.Remove(justVisited);
					this._Pipeline.Add(justVisited);
				}
				// Insert it before the shaders that need it if there are any
				else
				{
					this._Pipeline.Remove(justVisited);
					this._Pipeline.Insert(pipelineInsertIndex, justVisited);
				}
			}
		}

		public bool IsBuilt()
		{
			return this._Pipeline.Count > 0;
		}

		public void Run()
		{
			foreach (Abstractions.Shader step in this._Pipeline)
			{
				step.Run(this._ProcessingSize);
			}
		}

		public void Cleanup()
		{
			foreach (KeyValuePair<int, HashSet<GraphArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (GraphArcFromInputToShader arcData in inputArc.Value)
				{
					arcData.ToShader.UnbindUniform(arcData.ToShaderSlot);
				}
			}

			foreach (KeyValuePair<int, HashSet<GraphArcFromShaderToOutput>> outputArc in this._OutputGraph)
			{
				foreach (GraphArcFromShaderToOutput arcData in outputArc.Value)
				{
					arcData.FromShader.UnbindUniform(arcData.FromShaderSlot);
				}
			}

			foreach (KeyValuePair<Abstractions.Image, HashSet<GraphBufferBinding>> buffer in this._BufferBindings)
			{
				foreach (GraphBufferBinding binding in buffer.Value)
				{
					binding.Shader.UnbindUniform(binding.Slot);
				}

				buffer.Key.Cleanup();
			}

			this._BufferBindings.Clear();
			this._Pipeline.Clear();
		}
	}
}
