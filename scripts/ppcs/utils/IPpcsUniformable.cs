using Godot;

namespace Outlines.Ppcs.Utils
{
	public interface IPpcsUniformable
	{
		public Rid GetUniformableRid();
		public PpcsUniform CreateUniform(PpcsShader shader, int slot);
	}
}
