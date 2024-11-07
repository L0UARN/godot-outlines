using System;
using System.Collections.Generic;
using Godot;
using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class Graph : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private readonly Dictionary<Abstractions.Image, HashSet<GraphArcFromInputToShader>> _InputGraph = new();
		private readonly Dictionary<Abstractions.Shader, HashSet<GraphArcFromShaderToShader>> _ShaderGraph = new();
		private readonly Dictionary<Abstractions.Shader, HashSet<GraphArcFromShaderToOutput>> _OutputGraph = new();

		private Vector2I _BufferSize = Vector2I.Zero;
		private readonly Dictionary<Abstractions.Image, HashSet<GraphBufferBinding>> _BufferBindings = new();
		private readonly List<Abstractions.Shader> _Pipeline = new();

		public Graph(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void CreateArcFromInputToShader(Abstractions.Image fromInput, Abstractions.Shader toShader, int toShaderSlot)
		{
			if (!this._InputGraph.ContainsKey(fromInput))
			{
				this._InputGraph[fromInput] = new(1);
			}

			this._InputGraph[fromInput].Add(new(toShader, toShaderSlot));
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

		public void CreateArcFromShaderToOutput(Abstractions.Shader fromShader, int fromShaderSlot, Abstractions.Image toOutput)
		{
			if (!this._OutputGraph.ContainsKey(fromShader))
			{
				this._OutputGraph[fromShader] = new(1);
			}

			this._OutputGraph[fromShader].Add(new(fromShaderSlot, toOutput));
		}

		public void SetProcessingSize(Vector2I processingSize)
		{
			if (this._BufferSize.Equals(processingSize))
			{
				return;
			}

			if (processingSize.X <= 0 || processingSize.Y <= 0)
			{
				throw new Exception("A graph's processing size can't be less than zero.");
			}

			if (!this.IsBuilt())
			{
				this._BufferSize = processingSize;
				return;
			}

			foreach (KeyValuePair<Abstractions.Image, HashSet<GraphBufferBinding>> buffer in this._BufferBindings)
			{
				foreach (GraphBufferBinding binding in buffer.Value)
				{
					binding.Shader.UnbindUniform(binding.Slot);
				}

				buffer.Key.Size = processingSize;

				foreach (GraphBufferBinding binding in buffer.Value)
				{
					binding.Shader.BindUniform(buffer.Key, binding.Slot);
				}
			}

			this._BufferSize = processingSize;
		}

		public void Build()
		{
			if (this._BufferSize.Equals(Vector2I.Zero))
			{
				throw new Exception("Can't build the graph before having set the processing size.");
			}

			Queue<Abstractions.Shader> toVisit = new();

			foreach (KeyValuePair<Abstractions.Image, HashSet<GraphArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (GraphArcFromInputToShader arcDestination in inputArc.Value)
				{
					arcDestination.ToShader.BindUniform(inputArc.Key, arcDestination.ToShaderSlot);
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
							buffer = new(this._Rd, this._BufferSize);
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

				if (this._OutputGraph.ContainsKey(justVisited))
				{
					foreach (GraphArcFromShaderToOutput outputArc in this._OutputGraph[justVisited])
					{
						// Make it so `justVisited` outputs to the output image
						justVisited.BindUniform(outputArc.ToOutput, outputArc.FromShaderSlot);
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
				step.Run(this._BufferSize);
			}
		}

		public void Cleanup()
		{
			foreach (KeyValuePair<Abstractions.Image, HashSet<GraphArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (GraphArcFromInputToShader arcData in inputArc.Value)
				{
					arcData.ToShader.UnbindUniform(arcData.ToShaderSlot);
				}
			}

			foreach (KeyValuePair<Abstractions.Shader, HashSet<GraphArcFromShaderToOutput>> outputArc in this._OutputGraph)
			{
				foreach (GraphArcFromShaderToOutput arcData in outputArc.Value)
				{
					outputArc.Key.UnbindUniform(arcData.FromShaderSlot);
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
