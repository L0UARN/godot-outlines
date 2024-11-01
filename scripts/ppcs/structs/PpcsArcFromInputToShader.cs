using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsArcFromInputToShader
	{
		public PpcsShader ToShader { get; set; } = null;
		public int ToShaderSlot { get; set; } = 0;

		public PpcsArcFromInputToShader(PpcsShader toShader, int toShaderSlot)
		{
			this.ToShader = toShader;
			this.ToShaderSlot = toShaderSlot;
		}
	}
}
