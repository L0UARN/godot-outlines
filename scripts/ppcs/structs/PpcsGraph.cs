using System.Collections.Generic;
using System.Linq;
using Godot;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraph
	{
		private RenderingDevice _Rd = null;
		private Dictionary<PpcsImage, HashSet<PpcsArcFromInputToShader>> _InputGraph = new();
		private Dictionary<PpcsShader, HashSet<PpcsArcFromShaderToShader>> _ShaderGraph = new();
		private Dictionary<PpcsShader, HashSet<PpcsArcFromShaderToOutput>> _OutputGraph = new();
		private List<PpcsImage> _BufferPool = new();

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

		public void Build()
		{
			List<PpcsShader> pipeline = new();
			Stack<PpcsShader> toVisit = new();

			foreach (KeyValuePair<PpcsImage, HashSet<PpcsArcFromInputToShader>> inputArc in this._InputGraph)
			{
				foreach (PpcsArcFromInputToShader arcDestination in inputArc.Value)
				{
					arcDestination.ToShader.BindUniform(inputArc.Key, arcDestination.ToShaderSlot);
					toVisit.Push(arcDestination.ToShader);
				}
			}

			while (toVisit.Count > 0)
			{
				PpcsShader justVisited = toVisit.Pop();
				int pipelineInsertIndex = -1;

				// Explore the shaders that depend on `justVisited`
				if (this._ShaderGraph.ContainsKey(justVisited))
				{
					foreach (PpcsArcFromShaderToShader shaderArc in this._ShaderGraph[justVisited])
					{
						int toShaderIndex = pipeline.IndexOf(shaderArc.ToShader);

						// No need to visit a shader that has already been visited
						if (toShaderIndex != -1)
						{
							toVisit.Push(shaderArc.ToShader);
						}

						// If the shader that depends on `justVisited` is before `justVisited` in the pipeline, then make it so `justVisited` is inserted before it
						if (toShaderIndex != -1 && (toShaderIndex == -1 || toShaderIndex < pipelineInsertIndex))
						{
							pipelineInsertIndex = toShaderIndex;
						}

						// Bind the output of `justVisited` to the input of the shader that depends on it
						PpcsImage buffer = new(this._Rd, Vector2I.Zero);
						justVisited.BindUniform(buffer, shaderArc.FromShaderSlot);
						shaderArc.ToShader.BindUniform(buffer, shaderArc.ToShaderSlot);
						this._BufferPool.Add(buffer);
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
					pipeline.Add(justVisited);
				}
				// Insert it before the shaders that need it if there are any
				else
				{
					pipeline.Insert(pipelineInsertIndex, justVisited);
				}
			}
		}
	}
}
