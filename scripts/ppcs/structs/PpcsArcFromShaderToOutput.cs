using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsArcFromShaderToOutput
	{
		public int FromShaderSlot { get; private set; } = 0;
		public PpcsImage ToOutput { get; private set; } = null;

		public PpcsArcFromShaderToOutput(int fromShaderSlot, PpcsImage toOutput)
		{
			this.FromShaderSlot = fromShaderSlot;
			this.ToOutput = toOutput;
		}
	}
}
