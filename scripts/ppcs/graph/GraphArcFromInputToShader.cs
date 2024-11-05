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
	}
}
