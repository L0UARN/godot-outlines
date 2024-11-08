using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class GraphArcFromInputToShader
	{
		public Shader ToShader { get; private set; } = null;
		public int ToShaderSlot { get; private set; } = 0;

		public GraphArcFromInputToShader(Shader toShader, int toShaderSlot)
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

		// override object.GetHashCode
		public override int GetHashCode()
		{
			return System.HashCode.Combine(this.ToShader, this.ToShaderSlot);
		}
	}
}
