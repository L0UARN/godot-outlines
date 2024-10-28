using Godot;

namespace Outlines.Ppcs.Utils
{
	public interface IPpcsUniformable
	{
		public Rid CreateUniform(PpcsShader shader, int slot);
	}
}
