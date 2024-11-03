using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsArcFromInputToShader
	{
		public PpcsShader ToShader { get; private set; } = null;
		public int ToShaderSlot { get; private set; } = 0;

		public PpcsArcFromInputToShader(PpcsShader toShader, int toShaderSlot)
		{
			this.ToShader = toShader;
			this.ToShaderSlot = toShaderSlot;
		}
	}
}
