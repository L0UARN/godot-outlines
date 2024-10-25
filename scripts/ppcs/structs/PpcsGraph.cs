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

		// TODO: remove this once done debugging
		private void PrintGraph()
		{
			GD.Print("PpcsGraph:");
			foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
			{
				GD.Print("-> ", entry.Key, ":");
				foreach (PpcsShader value in entry.Value)
				{
					GD.Print("---> ", value);
				}
			}
		}

		public void BuildPipeline()
		{
			this._Pipeline.Clear();

			if (this._FinalNode == null)
			{
				throw new Exception("A PpcsGraph's FinalNode cannot be null");
			}
			
			Stack<PpcsShader> nodesToExplore = new();
			nodesToExplore.Push(FinalNode);

			while (nodesToExplore.Count > 0)
			{
				PpcsShader currentNode = nodesToExplore.Pop();
				this._Pipeline.Add(currentNode);

				foreach (KeyValuePair<PpcsShader, HashSet<PpcsShader>> entry in this._Graph)
				{
					if (entry.Value.Contains(currentNode))
					{
						nodesToExplore.Push(entry.Key);
					}
				}
			}

			this._Pipeline.Reverse();
		}

		private void CreateBufferPool()
		{
			// TODO: based on the pipeline and the graph, create the appropriate amount of buffers (idealy the minimal amount of buffers)
		}

		public PpcsGraph(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}
	}
};