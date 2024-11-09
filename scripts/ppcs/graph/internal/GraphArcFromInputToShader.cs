using Ppcs.Abstractions;
using System;

namespace Ppcs.Graph.Internal
{
	public class GraphArcFromInputToShader
	{
		public ComputeShader ToShader { get; private set; } = null;
		public int ToShaderSlot { get; private set; } = 0;

		public GraphArcFromInputToShader(ComputeShader toShader, int toShaderSlot)
		{
			this.ToShader = toShader;
			this.ToShaderSlot = toShaderSlot;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is GraphArcFromInputToShader other)
			{
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
			return HashCode.Combine(this.ToShader, this.ToShaderSlot);
		}
	}
}
