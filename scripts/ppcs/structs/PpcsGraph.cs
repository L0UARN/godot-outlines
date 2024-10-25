using System;
using System.Collections.Generic;
using Godot;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraph
	{
		private RenderingDevice _Rd = null;
		private Dictionary<PpcsShader, HashSet<PpcsShader>> _Graph = new();
		private List<PpcsShader> _Pipeline = new();
		private List<PpcsImage> _BufferPool = new();
		
		private PpcsShader _FinalNode = null;
		public PpcsShader FinalNode
		{
			get => _FinalNode;
			set
			{
				if (value == this._FinalNode)
				{
					return;
				}

				this._FinalNode = value;
			}
		}

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

		private void BuildPipeline()
		{
			this._Pipeline.Clear();

			Dictionary<PpcsShader, int> allowedAppearences = new();
			Dictionary<PpcsShader, int> currentAppearences = new();

			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				foreach (PpcsShader node in entry.Value)
				{
					if (!allowedAppearences.ContainsKey(node))
					{
						currentAppearences[node] = 0;
						allowedAppearences[node] = 1;
						continue;
					}

					allowedAppearences[node]++;
				}
			}

			Stack<PpcsShader> toVisit = new();
			toVisit.Push(this._FinalNode);

			while (toVisit.Count > 0)
			{
				PpcsShader currentNode = toVisit.Pop();

				// Detect cycles in the graph
				currentAppearences[currentNode]++;
				if (currentAppearences[currentNode] > allowedAppearences[currentNode])
				{
					throw new Exception("Cycle detected in PpcsGraph");
				}
				
				// Put the current node at the end of the pipeline
				this._Pipeline.Remove(currentNode);
				this._Pipeline.Add(currentNode);

				// Plan to visit all nodes that lead to the current node
				foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
				{
					if (entry.Value.Contains(currentNode))
					{
						toVisit.Push(entry.Key);
					}
				}
			}

			this._Pipeline.Reverse();
		}

		private void CreateBufferPool()
		{
			// TODO: based on the pipeline and the graph, create the appropriate amount of buffers (idealy the minimal amount of buffers)
		}
	}
};