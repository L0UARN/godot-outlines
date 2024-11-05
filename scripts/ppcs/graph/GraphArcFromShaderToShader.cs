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
	}
}
