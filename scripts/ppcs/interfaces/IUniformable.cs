using Godot;
using Ppcs.Abstractions;

namespace Ppcs.Interfaces
{
	public interface IUniformable
	{
		public Rid GetUniformableRid();
		public ComputeShaderUniform CreateUniform(ComputeShader shader, int slot);
	}
}
