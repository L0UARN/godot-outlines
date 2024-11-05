using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class GraphArcFromShaderToOutput
	{
		public int FromShaderSlot { get; private set; } = 0;
		public Image ToOutput { get; private set; } = null;

		public GraphArcFromShaderToOutput(int fromShaderSlot, Image toOutput)
		{
			this.FromShaderSlot = fromShaderSlot;
			this.ToOutput = toOutput;
		}
	}
}
