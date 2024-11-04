using System.Collections.Generic;
using Godot;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraph : IPpcsCleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private readonly Dictionary<PpcsImage, HashSet<PpcsArcFromInputToShader>> _InputGraph = new();
		private readonly Dictionary<PpcsShader, HashSet<PpcsArcFromShaderToShader>> _ShaderGraph = new();
		private readonly Dictionary<PpcsShader, HashSet<PpcsArcFromShaderToOutput>> _OutputGraph = new();

		private Vector2I _BufferSize = Vector2I.Zero;
		private readonly Dictionary<PpcsGraphBufferBinding, PpcsImage> _BoundBuffers = new();
		private readonly List<PpcsShader> _Pipeline = new();

		public PpcsGraph(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void CreateArcFromInputToShader(PpcsImage fromInput, PpcsShader toShader, int toShaderSlot)
		{
			if (!this._InputGraph.ContainsKey(fromInput))
			{
				this._InputGraph[fromInput] = new(1);
			}

			this._InputGraph[fromInput].Add(new(toShader, toShaderSlot));
		}

		public void CreateArcFromShaderToShader(PpcsShader fromShader, int fromShaderSlot, PpcsShader toShader, int toShaderSlot)
		{
			// TODO: check if adding this arc would make the graph cyclic, and throw an exception if so

			if (!this._ShaderGraph.ContainsKey(fromShader))
			{
				this._ShaderGraph[fromShader] = new(1);
			}

			this._ShaderGraph[fromShader].Add(new(fromShaderSlot, toShader, toShaderSlot));
		}

		public void CreateArcFromShaderToOutput(PpcsShader fromShader, int fromShaderSlot, PpcsImage toOutput)
		{
			if (!this._OutputGraph.ContainsKey(fromShader))
			{
				this._OutputGraph[fromShader] = new(1);
			}

			this._OutputGraph[fromShader].Add(new(fromShaderSlot, toOutput));
		}

		public bool IsBuilt()
		{
			return this._Pipeline.Count > 0;
		}

		public void Build()
		{
			// The image buffers must be large enough to process all of the input images
			this._BufferSize = Vector2I.Zero;

			foreach (PpcsImage inputImage in this._InputGraph.Keys)
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

			Queue<PpcsShader> toVisit = new();

			foreach (KeyValuePair<PpcsImage, HashSet<PpcsArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (PpcsArcFromInputToShader arcDestination in inputArc.Value)
				{
					arcDestination.ToShader.BindUniform(inputArc.Key, arcDestination.ToShaderSlot);
					toVisit.Enqueue(arcDestination.ToShader);
				}
			}

			while (toVisit.Count > 0)
			{
				PpcsShader justVisited = toVisit.Dequeue();
				int pipelineInsertIndex = -1;

				// Explore the shaders that depend on `justVisited`
				if (this._ShaderGraph.ContainsKey(justVisited))
				{
					foreach (PpcsArcFromShaderToShader shaderArc in this._ShaderGraph[justVisited])
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
						PpcsImage buffer = null;
						PpcsGraphBufferBinding bufferBinding = new(justVisited, shaderArc.FromShaderSlot);

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
					foreach (PpcsArcFromShaderToOutput outputArc in this._OutputGraph[justVisited])
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
			foreach (PpcsShader step in this._Pipeline)
			{
				step.Run(this._BufferSize);
			}
		}

		public void Cleanup()
		{
			foreach (KeyValuePair<PpcsImage, HashSet<PpcsArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (PpcsArcFromInputToShader arcData in inputArc.Value)
				{
					arcData.ToShader.UnbindUniform(arcData.ToShaderSlot);
				}
			}

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsArcFromShaderToShader>> shaderArc in this._ShaderGraph)
			{
				foreach (PpcsArcFromShaderToShader arcData in shaderArc.Value)
				{
					shaderArc.Key.UnbindUniform(arcData.FromShaderSlot);
					arcData.ToShader.UnbindUniform(arcData.ToShaderSlot);
				}
			}

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsArcFromShaderToOutput>> outputArc in this._OutputGraph)
			{
				foreach (PpcsArcFromShaderToOutput arcData in outputArc.Value)
				{
					outputArc.Key.UnbindUniform(arcData.FromShaderSlot);
				}
			}

			foreach (KeyValuePair<PpcsGraphBufferBinding, PpcsImage> boundBuffer in this._BoundBuffers)
			{
				boundBuffer.Value.Cleanup();
			}

			this._BoundBuffers.Clear();
			this._Pipeline.Clear();
		}
	}
}
