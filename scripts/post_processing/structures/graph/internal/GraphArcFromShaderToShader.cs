using System;
using PostProcessing.Abstractions;

namespace PostProcessing.Structures.Graph.Internal
{
	public class GraphArcFromShaderToShader
	{
		public int FromShaderSlot { get; private set; } = 0;
		public ComputeShader ToShader { get; private set; } = null;
		public int ToShaderSlot { get; private set; } = 0;

		public GraphArcFromShaderToShader(int fromShaderSlot, ComputeShader toShader, int toShaderSlot)
		{
			this.FromShaderSlot = fromShaderSlot;
			this.ToShader = toShader;
			this.ToShaderSlot = toShaderSlot;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is GraphArcFromShaderToShader other)
			{
				if (!other.FromShaderSlot.Equals(this.FromShaderSlot))
				{
					return false;
				}

				if (!other.ToShader.Equals(this.ToShader))
				{
					return false;
				}

				if (!other.ToShaderSlot.Equals(this.ToShaderSlot))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.FromShaderSlot, this.ToShader, this.ToShaderSlot);
		}
	}
}
