using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraph
	{
		private RenderingDevice _Rd = null;
		private Dictionary<PpcsShader, HashSet<PpcsShader>> _Graph = new();
		private List<PpcsShader> _StartingNodes = new();
		private PpcsShader _EndingNode = null;
		private List<PpcsImage> _BufferPool = new();

		public void AddArc(PpcsShader fromNode, PpcsShader toNode)
		{
			if (!this._Graph.ContainsKey(fromNode))
			{
				this._Graph[fromNode] = new();
			}

			this._Graph[fromNode].Add(toNode);
		}

		public void RemoveArc(PpcsShader fromNode, PpcsShader toNode)
		{
			if (!this._Graph.ContainsKey(fromNode))
			{
				return;
			}

			this._Graph[fromNode].Remove(toNode);
		}

		public void AddNode(PpcsShader node)
		{
			if (this._Graph.ContainsKey(node))
			{
				return;
			}

			this._Graph[node] = new();
		}

		public void RemoveNode(PpcsShader node)
		{
			this._Graph.Remove(node);

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				entry.Value.Remove(node);
			}
		}

		public void FindStartsAndEnd()
		{
			this._StartingNodes.AddRange(this._Graph.Keys);
			this._EndingNode = null;

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				// Iterate through the successors of entry.Key
				foreach (PpcsShader node in entry.Value)
				{
					// If a node is a successor of another, then it can't be a starting node
					this._StartingNodes.Remove(node);

					// If a node is a successor of another, and isn't the predecessor of any, then it's the ending node
					if (!this._Graph.ContainsKey(node))
					{
						// There can't be multiple ending nodes for the graph (the goal is to output one image)
						if (this._EndingNode != node && this._EndingNode != null)
						{
							throw new Exception("PpcsGraph has multiple ending nodes (must have exactly one).");
						}

						this._EndingNode = node;
					}
				}
			}

			if (this._StartingNodes.Count == 0)
			{
				throw new Exception("PpcsGraph has no starting nodes (must have at least one).");
			}

			if (this._EndingNode == null)
			{
				throw new Exception("PpcsGraph has no ending node (must have exactly one).");
			}
		}

		public void CheckForCycles()
		{
			foreach (PpcsShader startingNode in this._StartingNodes)
			{
				Stack<PpcsShader> toVisit = new();
				toVisit.Push(startingNode);
				HashSet<PpcsShader> visited = new();

				while (toVisit.Count > 0)
				{
					PpcsShader currentlyVisiting = toVisit.Pop();
					visited.Add(currentlyVisiting);

					if (!this._Graph.ContainsKey(currentlyVisiting))
					{
						continue;
					}

					foreach (PpcsShader successor in this._Graph[currentlyVisiting])
					{
						if (toVisit.Contains(successor))
						{
							throw new Exception("PpcsGraph contains a cycle (each shader can only be ran once).");
						}

						toVisit.Push(successor);
					}
				}
			}
		}

		public void CheckForIsolatedNodes()
		{
			// 1. Gather all the nodes present in the graph
			// 2. Build a reversed version of the graph

			HashSet<PpcsShader> allNodes = new();
			Dictionary<PpcsShader, HashSet<PpcsShader>> reverseGraph = new();

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				allNodes.Add(entry.Key);

				foreach (PpcsShader node in entry.Value)
				{
					allNodes.Add(node);

					if (!reverseGraph.ContainsKey(node))
					{
						reverseGraph[node] = new();
					}

					reverseGraph[node].Add(entry.Key);
				}
			}

			// 2. Go through the graph in reverse to find all accessible nodes

			Stack<PpcsShader> toVisit = new();
			toVisit.Push(this._EndingNode);
			HashSet<PpcsShader> visited = new();

			while (toVisit.Count > 0)
			{
				PpcsShader currentlyVisiting = toVisit.Pop();
				visited.Add(currentlyVisiting);

				if (!reverseGraph.ContainsKey(currentlyVisiting))
				{
					continue;
				}

				foreach (PpcsShader predecessor in reverseGraph[currentlyVisiting])
				{
					toVisit.Push(predecessor);
				}
			}

			// 3. Compare the two sets to see if all nodes are accessible

			if (!visited.SetEquals(allNodes))
			{
				throw new Exception("PpcsGraph has isolated nodes (must have none).");
			}
		}

		public void CreateBufferPool()
		{
			Dictionary<PpcsShader, int> predecessorCount = new();

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				foreach (PpcsShader node in entry.Value)
				{
					if (!predecessorCount.ContainsKey(node))
					{
						predecessorCount[node] = 1;
						continue;
					}

					predecessorCount[node]++;
				}
			}

			int bufferPoolSize = predecessorCount.Values.Max() + 1;

			for (int i = 0; i < bufferPoolSize; i++)
			{
				this._BufferPool.Add(null);
			}
		}

		public PpcsGraph(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}
	}
};
