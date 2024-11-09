using System;
using System.Collections.Generic;
using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class Graph : ICleanupable
	{
		private readonly Godot.RenderingDevice _Rd = null;

		private readonly Dictionary<int, Image> _Inputs = new();
		private readonly Dictionary<int, HashSet<GraphArcFromInputToShader>> _InputGraph = new();
		private readonly Dictionary<int, Image> _Outputs = new();
		private readonly Dictionary<int, HashSet<GraphArcFromShaderToOutput>> _OutputGraph = new();
		private readonly Dictionary<Shader, HashSet<GraphArcFromShaderToShader>> _ShaderGraph = new();

		private Godot.Vector2I _ProcessingSize = Godot.Vector2I.MinValue;
		public Godot.Vector2I ProcessingSize
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

				// Unbind all the shaders
				foreach (GraphBufferBinding binding in this._NewBufferBindings.Keys)
				{
					binding.Shader.UnbindUniform(binding.Slot);
				}

				// Resize each buffer and rebind them
				foreach (KeyValuePair<GraphBufferBinding, Image> binding in this._NewBufferBindings)
				{
					// If multiple shaders are bound to this buffer, the size will be set multiple times
					// But since there's a check that prevents from resizing to the same size, it's fine
					binding.Value.Size = value;
					binding.Key.Shader.BindUniform(binding.Value, binding.Key.Slot);
				}

				Godot.GD.Print("Resize");
				this._ProcessingSize = value;
			}
		}

		private readonly Dictionary<GraphBufferBinding, Image> _NewBufferBindings = new();
		// private readonly Dictionary<Image, HashSet<GraphBufferBinding>> _BufferBindings = new();
		private readonly List<Shader> _Pipeline = new();

		public Graph(Godot.RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void CreateArcFromInputToShader(int fromInput, Shader toShader, int toShaderSlot)
		{
			if (!this._InputGraph.ContainsKey(fromInput))
			{
				this._InputGraph[fromInput] = new(1);
			}

			this._InputGraph[fromInput].Add(new(toShader, toShaderSlot));

			// Bind the image if it's already known
			if (this._Inputs.ContainsKey(fromInput))
			{
				Godot.GD.Print($"Create: Binding input {fromInput} to shader {toShader} slot {toShaderSlot}");
				toShader.BindUniform(this._Inputs[fromInput], toShaderSlot);
			}
		}

		public void BindInput(int input, Image inputImage)
		{
			if (this._Inputs.TryGetValue(input, out Image previous))
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
					Godot.GD.Print($"Bind: Binding input {input} to shader ${arc.ToShader} slot {arc.ToShaderSlot}");
					arc.ToShader.BindUniform(inputImage, arc.ToShaderSlot);
				}
			}
		}

		public void CreateArcFromShaderToShader(Shader fromShader, int fromShaderSlot, Shader toShader, int toShaderSlot)
		{
			if (!this._ShaderGraph.ContainsKey(fromShader))
			{
				this._ShaderGraph[fromShader] = new(1);
			}

			GraphArcFromShaderToShader newArc = new(fromShaderSlot, toShader, toShaderSlot);
			this._ShaderGraph[fromShader].Add(newArc);

			// Check if creating the arc has created a cycle in the graph
			Stack<Shader> toVisit = new();
			toVisit.Push(fromShader);

			while (toVisit.Count > 0)
			{
				Shader justVisited = toVisit.Pop();

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

		public void CreateArcFromShaderToOutput(Shader fromShader, int fromShaderSlot, int toOutput)
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

		public void BindOutput(int output, Image outputImage)
		{
			if (this._Outputs.TryGetValue(output, out Image previous))
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

		private Dictionary<Shader, HashSet<Shader>> GetReversedShaderGraph()
		{
			Dictionary<Shader, HashSet<Shader>> result = new();

			foreach (KeyValuePair<Shader, HashSet<GraphArcFromShaderToShader>> arcs in this._ShaderGraph)
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

		private Stack<Shader> GetStartingShadersToVisit(Dictionary<Shader, HashSet<Shader>> reversedShaderGraph)
		{
			Stack<Shader> result = new();

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

		private void BindShadersWithBuffer(Shader fromShader, int fromShaderSlot, Shader toShader, int toShaderSlot)
		{
			// Find out if creating a new buffer is needed, or if there's already one for this output
			Image buffer = null;
			GraphBufferBinding fromBinding = new(fromShader, fromShaderSlot);

			if (this._NewBufferBindings.ContainsKey(fromBinding))
			{
				buffer = this._NewBufferBindings[fromBinding];
			}
			else
			{
				buffer = new(this._Rd, this._ProcessingSize);
				this._NewBufferBindings[fromBinding] = buffer;
			}

			// Bind the output of `justVisited` to the input of the shader that depends on it
			fromShader.BindUniform(buffer, fromShaderSlot);
			// Bind the input of the shader that depends on `justVisited`
			toShader.BindUniform(buffer, toShaderSlot);
			this._NewBufferBindings[new(toShader, toShaderSlot)] = buffer;
		}

		public void Build()
		{
			if (this._ProcessingSize.Equals(Godot.Vector2I.MinValue))
			{
				throw new Exception("Can't build the graph before having set the processing size.");
			}

			// this._ShaderGraph contains all the shaders that are dependent on one shader
			// reversedShaderGraph contains all the shader that one shader is dependent on
			Dictionary<Shader, HashSet<Shader>> reversedShaderGraph = this.GetReversedShaderGraph();
			// Start visiting the shader graph starting with the ones that use an input image
			Stack<Shader> toVisit = this.GetStartingShadersToVisit(reversedShaderGraph);

			while (toVisit.Count > 0)
			{
				Shader justVisited = toVisit.Pop();
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
					foreach (Shader dependency in reversedShaderGraph[justVisited])
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

			Godot.GD.Print("Pipeline:");
			foreach (Shader shader in this._Pipeline)
			{
				Godot.GD.Print($"-> {shader}");
			}
		}

		public bool IsBuilt()
		{
			return this._Pipeline.Count > 0;
		}

		public void Run()
		{
			foreach (Shader step in this._Pipeline)
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

			// Unbind all the buffers from the shaders before cleaning up the buffers
			// This way there are no invalid uniforms at any point
			foreach (GraphBufferBinding binding in this._NewBufferBindings.Keys)
			{
				binding.Shader.UnbindUniform(binding.Slot);
			}

			foreach (Image buffer in this._NewBufferBindings.Values)
			{
				buffer.Cleanup();
			}

			this._NewBufferBindings.Clear();
			this._Pipeline.Clear();
		}
	}
}
