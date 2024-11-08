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
	}
}
