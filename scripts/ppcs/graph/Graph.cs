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
		private readonly Dictionary<GraphBufferBinding, Abstractions.Image> _BoundBuffers = new();
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

		public void Build()
		{
			// The image buffers must be large enough to process all of the input images
			this._BufferSize = Vector2I.Zero;

			foreach (Abstractions.Image inputImage in this._InputGraph.Keys)
			{
				if (this._BufferSize.Equals(Vector2I.Zero))
				{
					this._BufferSize = inputImage.Size;
					continue;
				}

				if (inputImage.Size.X > this._BufferSize.X)
				{
					this._BufferSize.X = inputImage.Size.X;
				}

				if (inputImage.Size.Y > this._BufferSize.Y)
				{
					this._BufferSize.Y = inputImage.Size.Y;
				}
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

						// Bind the output of `justVisited` to the input of the shader that depends on it
						Abstractions.Image buffer = null;
						GraphBufferBinding bufferBinding = new(justVisited, shaderArc.FromShaderSlot);

						if (this._BoundBuffers.ContainsKey(bufferBinding))
						{
							buffer = this._BoundBuffers[bufferBinding];
						}
						else
						{
							buffer = new(this._Rd, this._BufferSize);
							this._BoundBuffers[bufferBinding] = buffer;
						}

						justVisited.BindUniform(buffer, shaderArc.FromShaderSlot);
						shaderArc.ToShader.BindUniform(buffer, shaderArc.ToShaderSlot);
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

			foreach (KeyValuePair<Abstractions.Shader, HashSet<GraphArcFromShaderToShader>> shaderArc in this._ShaderGraph)
			{
				foreach (GraphArcFromShaderToShader arcData in shaderArc.Value)
				{
					shaderArc.Key.UnbindUniform(arcData.FromShaderSlot);
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

			foreach (KeyValuePair<GraphBufferBinding, Abstractions.Image> boundBuffer in this._BoundBuffers)
			{
				boundBuffer.Value.Cleanup();
			}

			this._BoundBuffers.Clear();
			this._Pipeline.Clear();
		}
	}
}
