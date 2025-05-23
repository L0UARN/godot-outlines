using System;
using System.Collections.Generic;
using PostProcessing.Abstractions;
using PostProcessing.Structures.Graph.Internal;
using PostProcessing.Behavior;
using Godot;

namespace PostProcessing.Structures.Graph
{
	public class Graph : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private readonly Dictionary<int, ImageBuffer> _Inputs = [];
		private readonly Dictionary<int, HashSet<GraphArcFromInputToShader>> _InputGraph = [];
		private readonly Dictionary<int, ImageBuffer> _Outputs = [];
		private readonly Dictionary<int, HashSet<GraphArcFromShaderToOutput>> _OutputGraph = [];
		private readonly Dictionary<ComputeShader, HashSet<GraphArcFromShaderToShader>> _ShaderGraph = [];
		private readonly Dictionary<GraphBufferBinding, ImageBuffer> _ImageBufferBindings = [];
		private readonly List<ComputeShader> _Pipeline = [];
		public bool IsBuilt { get; private set; } = false;

		private Vector2I _ProcessingSize = Vector2I.MinValue;
		private Vector2I ProcessingSize
		{
			get => this._ProcessingSize;
			set
			{
				if (value.X <= 0 || value.Y <= 0)
				{
					throw new Exception("A graph's processing size can't be less than zero.");
				}

				if (this._ProcessingSize.Equals(value))
				{
					return;
				}

				// If the graph has already been built, the buffers need to be resized
				if (this.IsBuilt)
				{
					// Unbind all the buffer
					foreach (GraphBufferBinding binding in this._ImageBufferBindings.Keys)
					{
						binding.Shader.UnbindUniform(binding.Slot);
					}

					// Resize each buffer and rebind them
					foreach (KeyValuePair<GraphBufferBinding, ImageBuffer> binding in this._ImageBufferBindings)
					{
						// If multiple shaders are bound to this buffer, the size will be set multiple times
						// But since there's a check that prevents from resizing to the same size, it's fine
						binding.Value.Size = value;
						binding.Key.Shader.BindUniform(binding.Value, binding.Key.Slot);
					}
				}

				this._ProcessingSize = value;
			}
		}

		public Graph(RenderingDevice renderingDevice)
		{
			if (renderingDevice == null)
			{
				throw new Exception("The graph needs a non-null rendering device in order to work.");
			}

			this._Rd = renderingDevice;
		}

		public void CreateArcFromInputToShader(int fromInput, ComputeShader toShader, int toShaderSlot)
		{
			if (this.IsBuilt)
			{
				throw new Exception("Can't edit the graph once it has been built.");
			}

			if (toShader == null)
			{
				throw new Exception("Can't create an arc to a null shader.");
			}

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

		private void UpdateProcessingSize()
		{
			Vector2I idealSize = Vector2I.MinValue;

			foreach (ImageBuffer buffer in this._Inputs.Values)
			{
				idealSize.X = Math.Max(idealSize.X, buffer.Size.X);
				idealSize.Y = Math.Max(idealSize.Y, buffer.Size.Y);
			}

			foreach (ImageBuffer buffer in this._Outputs.Values)
			{
				idealSize.X = Math.Max(idealSize.X, buffer.Size.X);
				idealSize.Y = Math.Max(idealSize.Y, buffer.Size.Y);
			}

			this.ProcessingSize = idealSize;
		}

		public void BindInput(int input, ImageBuffer inputImage)
		{
			if (this._Inputs.TryGetValue(input, out ImageBuffer previous))
			{
				if (previous.Equals(inputImage))
				{
					return;
				}
			}

			this._Inputs[input] = inputImage;
			this.UpdateProcessingSize();

			// Bind the new input image to all the shaders that require it
			if (this._InputGraph.TryGetValue(input, out HashSet<GraphArcFromInputToShader> arcs))
			{
				foreach (GraphArcFromInputToShader arc in arcs)
				{
					arc.ToShader.BindUniform(inputImage, arc.ToShaderSlot);
				}
			}
		}

		private bool CheckForCycle(ComputeShader fromShader)
		{
			Stack<ComputeShader> toVisit = new();
			toVisit.Push(fromShader);

			while (toVisit.Count > 0)
			{
				ComputeShader justVisited = toVisit.Pop();

				if (!this._ShaderGraph.ContainsKey(justVisited))
				{
					continue;
				}

				foreach (GraphArcFromShaderToShader arc in this._ShaderGraph[justVisited])
				{
					if (arc.ToShader.Equals(fromShader))
					{
						return true;
					}

					toVisit.Push(arc.ToShader);
				}
			}

			return false;
		}

		public void CreateArcFromShaderToShader(ComputeShader fromShader, int fromShaderSlot, ComputeShader toShader, int toShaderSlot)
		{
			if (this.IsBuilt)
			{
				throw new Exception("Can't edit the graph once it has been built.");
			}

			if (fromShader == null)
			{
				throw new Exception("Can't create an arc from null shader.");
			}

			if (toShader == null)
			{
				throw new Exception("Can't create an arc to a null shader.");
			}

			if (!this._ShaderGraph.ContainsKey(fromShader))
			{
				this._ShaderGraph[fromShader] = new(1);
			}

			GraphArcFromShaderToShader newArc = new(fromShaderSlot, toShader, toShaderSlot);
			this._ShaderGraph[fromShader].Add(newArc);

			// Check if creating this new arc would cause the graph to become cyclic
			if (this.CheckForCycle(fromShader))
			{
				this._ShaderGraph[fromShader].Remove(newArc);
				throw new Exception("Creating this arc would create a cycle in the graph.");
			}
		}

		public void CreateArcFromShaderToOutput(ComputeShader fromShader, int fromShaderSlot, int toOutput)
		{
			if (this.IsBuilt)
			{
				throw new Exception("Can't edit the graph once it has been built.");
			}

			if (fromShader == null)
			{
				throw new Exception("Can't create an arc from a null shader.");
			}

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

		public void BindOutput(int output, ImageBuffer outputImage)
		{
			if (this._Outputs.TryGetValue(output, out ImageBuffer previous))
			{
				if (previous.Equals(outputImage))
				{
					return;
				}
			}

			this._Outputs[output] = outputImage;
			this.UpdateProcessingSize();

			// Bind the new output image to all the shaders that require it
			if (this._OutputGraph.TryGetValue(output, out HashSet<GraphArcFromShaderToOutput> arcs))
			{
				foreach (GraphArcFromShaderToOutput arc in arcs)
				{
					arc.FromShader.BindUniform(outputImage, arc.FromShaderSlot);
				}
			}
		}

		private Dictionary<ComputeShader, HashSet<ComputeShader>> GetReversedShaderGraph()
		{
			Dictionary<ComputeShader, HashSet<ComputeShader>> result = new();

			foreach (KeyValuePair<ComputeShader, HashSet<GraphArcFromShaderToShader>> arcs in this._ShaderGraph)
			{
				foreach (GraphArcFromShaderToShader arc in arcs.Value)
				{
					if (!result.ContainsKey(arc.ToShader))
					{
						result[arc.ToShader] = new(1);
					}

					result[arc.ToShader].Add(arcs.Key);
				}
			}

			return result;
		}

		private Stack<ComputeShader> GetStartingShadersToVisit(Dictionary<ComputeShader, HashSet<ComputeShader>> reversedShaderGraph)
		{
			Stack<ComputeShader> result = new();

			foreach (KeyValuePair<int, HashSet<GraphArcFromInputToShader>> arcs in this._InputGraph)
			{
				foreach (GraphArcFromInputToShader arc in arcs.Value)
				{
					if (!reversedShaderGraph.ContainsKey(arc.ToShader))
					{
						result.Push(arc.ToShader);
					}
				}
			}

			return result;
		}

		private void BindShadersWithBuffer(ComputeShader fromShader, int fromShaderSlot, ComputeShader toShader, int toShaderSlot)
		{
			// Find out if creating a new buffer is needed, or if there's already one for this output
			ImageBuffer buffer = null;
			GraphBufferBinding fromBinding = new(fromShader, fromShaderSlot);

			if (this._ImageBufferBindings.ContainsKey(fromBinding))
			{
				buffer = this._ImageBufferBindings[fromBinding];
			}
			else
			{
				buffer = new(this._Rd, this.ProcessingSize);
				this._ImageBufferBindings[fromBinding] = buffer;
			}

			// Bind the output of `justVisited` to the input of the shader that depends on it
			fromShader.BindUniform(buffer, fromShaderSlot);
			// Bind the input of the shader that depends on `justVisited`
			toShader.BindUniform(buffer, toShaderSlot);
			this._ImageBufferBindings[new(toShader, toShaderSlot)] = buffer;
		}

		public void Build()
		{
			if (this.IsBuilt)
			{
				throw new Exception("The graph has already been built.");
			}

			if (this._Inputs.Count == 0 || this._Outputs.Count == 0 || this.ProcessingSize.Equals(Vector2I.MinValue))
			{
				throw new Exception("At least one input image and one output image must be bound before building the graph.");
			}

			if (this._InputGraph.Count == 0 || this._ShaderGraph.Count == 0 || this._OutputGraph.Count == 0)
			{
				throw new Exception("The graph requires at least one input to shader arc, one shader to shader arc, and one shader to output arc in order to be built.");
			}

			// this._ShaderGraph contains all the shaders that are dependent on one shader
			// reversedShaderGraph contains all the shader that one shader is dependent on
			Dictionary<ComputeShader, HashSet<ComputeShader>> reversedShaderGraph = this.GetReversedShaderGraph();
			// Start visiting the shader graph starting with the ones that use an input image
			Stack<ComputeShader> toVisit = this.GetStartingShadersToVisit(reversedShaderGraph);

			while (toVisit.Count > 0)
			{
				ComputeShader justVisited = toVisit.Pop();
				// These indices are needed to know where in the pipeline to insert the current shader
				int firstDependentIndex = -1;
				int lastDependencyIndex = -1;

				// Explore the shaders that depend on `justVisited`
				if (this._ShaderGraph.ContainsKey(justVisited))
				{
					foreach (GraphArcFromShaderToShader arcs in this._ShaderGraph[justVisited])
					{
						int dependentShaderIndex = this._Pipeline.IndexOf(arcs.ToShader);

						// Visit the shader that depends on `justVisited` (only if it has not been visited before)
						if (dependentShaderIndex == -1)
						{
							toVisit.Push(arcs.ToShader);
						}

						// Register the index of the dependent shader
						if (firstDependentIndex == -1 || (dependentShaderIndex != -1 && dependentShaderIndex < firstDependentIndex))
						{
							firstDependentIndex = dependentShaderIndex;
						}

						this.BindShadersWithBuffer(justVisited, arcs.FromShaderSlot, arcs.ToShader, arcs.ToShaderSlot);
					}
				}

				if (reversedShaderGraph.ContainsKey(justVisited))
				{
					foreach (ComputeShader dependency in reversedShaderGraph[justVisited])
					{
						int dependencyIndex = this._Pipeline.IndexOf(dependency);

						// Register the index of the dependency
						if (lastDependencyIndex == -1 || (dependencyIndex != -1 && dependencyIndex > lastDependencyIndex))
						{
							lastDependencyIndex = dependencyIndex;
						}
					}
				}

				// Add `justVisited` at the end of the pipeline if no shaders none of its dependencies or dependents are in the pipeline
				if (firstDependentIndex == -1 && lastDependencyIndex == -1)
				{
					this._Pipeline.Add(justVisited);
				}
				// Insert `justVisited` before the first shader that depends on it
				else if (firstDependentIndex != -1)
				{
					this._Pipeline.Insert(firstDependentIndex, justVisited);
				}
				// Insert `justVisisted` after the last shader that depends on it
				else if (lastDependencyIndex != -1)
				{
					this._Pipeline.Insert(lastDependencyIndex + 1, justVisited);
				}
			}

			this.IsBuilt = true;
		}

		public void Run()
		{
			if (!this.IsBuilt)
			{
				throw new Exception("Can't run the graph before it has been built.");
			}

			foreach (ComputeShader step in this._Pipeline)
			{
				step.Run(this.ProcessingSize);
			}
		}

		public void Cleanup()
		{
			this.IsBuilt = false;

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

			// Unbind all the buffers from the shaders before cleaning up the buffers
			// This way there are no invalid uniforms at any point
			foreach (GraphBufferBinding binding in this._ImageBufferBindings.Keys)
			{
				binding.Shader.UnbindUniform(binding.Slot);
			}

			foreach (ImageBuffer buffer in this._ImageBufferBindings.Values)
			{
				buffer.Cleanup();
			}

			this._ImageBufferBindings.Clear();
			this._Pipeline.Clear();
		}
	}
}
