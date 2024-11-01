using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsArcFromShaderToShader
	{
		public int FromShaderSlot { get; set; } = 0;
		public PpcsShader ToShader { get; set; } = null;
		public int ToShaderSlot { get; set; } = 0;

		public PpcsArcFromShaderToShader(int fromShaderSlot, PpcsShader toShader, int toShaderSlot)
		{
			this.FromShaderSlot = fromShaderSlot;
			this.ToShader = toShader;
			this.ToShaderSlot = toShaderSlot;
		}
	}
}
