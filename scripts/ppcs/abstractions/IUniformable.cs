using Godot;

namespace Ppcs.Abstractions
{
	public interface IUniformable
	{
		public Rid GetUniformableRid();
		public Uniform CreateUniform(Shader shader, int slot);
	}
}
