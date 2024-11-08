using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class GraphArcFromShaderToOutput
	{
		public Shader FromShader { get; private set; } = null;
		public int FromShaderSlot { get; private set; } = 0;

		public GraphArcFromShaderToOutput(Shader fromShader, int fromShaderSlot)
		{
			this.FromShader = fromShader;
			this.FromShaderSlot = fromShaderSlot;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is GraphArcFromShaderToOutput other)
			{
				if (!other.FromShader.Equals(this.FromShader))
				{
					return false;
				}

				if (!other.FromShaderSlot.Equals(this.FromShaderSlot))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return System.HashCode.Combine(this.FromShader, this.FromShaderSlot);
		}
	}
}
