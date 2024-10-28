using System;
using System.Collections.Generic;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraph2
	{
		private class PpcsGraphNode
		{
			public PpcsShader Shader { get; set; } = null;
			public int Slot { get; set; } = 0;

			public PpcsGraphNode(PpcsShader shader, int slot)
			{
				this.Shader = shader;
				this.Slot = slot;
			}

			public override bool Equals(object obj)
			{
				if (obj != null && obj is PpcsGraphNode otherGraphNode)
				{
					if (!this.Shader.Rid.Equals(otherGraphNode.Shader.Rid))
					{
						return false;
					}

					if (!this.Slot.Equals(otherGraphNode.Slot))
					{
						return false;
					}

					return true;
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				return (this.Shader.Rid, this.Slot).GetHashCode();
			}
		}

		private Dictionary<PpcsImage, HashSet<PpcsGraphNode>> _InputGraph = new();
		private Dictionary<PpcsGraphNode, HashSet<PpcsGraphNode>> _NodeGraph = new();
		private Dictionary<PpcsGraphNode, HashSet<PpcsImage>> _OutputGraph = new();

		public void CreateArcFromInputToShader(PpcsImage fromInput, PpcsShader toShader, int toShaderSlot)
		{
			if (!this._InputGraph.ContainsKey(fromInput))
			{
				this._InputGraph[fromInput] = new(1);
			}

			PpcsGraphNode node = new(toShader, toShaderSlot);
			this._InputGraph[fromInput].Add(node);
		}

		public void CreateArcFromShaderToShader(PpcsShader fromShader, int fromShaderSlot, PpcsShader toShader, int toShaderSlot)
		{
			PpcsGraphNode fromNode = new(fromShader, fromShaderSlot);

			if (!this._NodeGraph.ContainsKey(fromNode))
			{
				this._NodeGraph[fromNode] = new(1);
			}

			PpcsGraphNode toNode = new(toShader, toShaderSlot);
			this._NodeGraph[fromNode].Add(toNode);
		}

		public void CreateArcFromNodeToOutput(PpcsShader fromShader, int fromShaderSlot, PpcsImage toOutput)
		{
			PpcsGraphNode fromNode = new(fromShader, fromShaderSlot);

			if (!this._OutputGraph.ContainsKey(fromNode))
			{
				this._OutputGraph[fromNode] = new(1);
			}

			this._OutputGraph[fromNode].Add(toOutput);
		}

		public void CheckForCyclesAndUnreachableEndingNodes()
		{
			HashSet<PpcsGraphNode> startingNodes = new();

			foreach (KeyValuePair<PpcsImage, HashSet<PpcsGraphNode>> entry in this._InputGraph)
			{
				startingNodes.UnionWith(entry.Value);
			}

			HashSet<PpcsGraphNode> reachableEndingNodes = new();

			foreach (PpcsGraphNode startingNode in startingNodes)
			{
				Stack<PpcsGraphNode> toVisit = new();
				toVisit.Push(startingNode);
				HashSet<PpcsGraphNode> visited = new();

				while (toVisit.Count > 0)
				{
					PpcsGraphNode currentylVisiting = toVisit.Pop();
					visited.Add(currentylVisiting);

					if (!this._NodeGraph.ContainsKey(currentylVisiting))
					{
						continue;
					}

					foreach (PpcsGraphNode successor in this._NodeGraph[currentylVisiting])
					{
						if (toVisit.Contains(successor))
						{
							throw new Exception("PpcsGraph is cyclic.");
						}

						toVisit.Push(successor);
					}
				}

				visited.IntersectWith(this._OutputGraph.Keys);
				reachableEndingNodes.UnionWith(visited);
			}

			if (!reachableEndingNodes.SetEquals(this._OutputGraph.Keys))
			{
				throw new Exception("Some output nodes of the PpcsGraph aren't reachable.");
			}
		}

		// TODO: check for non-contributing nodes (nodes that don't connect an input to an output)
		// TODO: create a pool of buffer images
		// TODO: run the graph
	}
}
