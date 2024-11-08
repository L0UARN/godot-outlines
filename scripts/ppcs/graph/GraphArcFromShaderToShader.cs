using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class GraphArcFromShaderToShader
	{
		public int FromShaderSlot { get; private set; } = 0;
		public Shader ToShader { get; private set; } = null;
		public int ToShaderSlot { get; private set; } = 0;

		public GraphArcFromShaderToShader(int fromShaderSlot, Shader toShader, int toShaderSlot)
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
			return System.HashCode.Combine(this.FromShaderSlot, this.ToShader, this.ToShaderSlot);
		}
	}
}
