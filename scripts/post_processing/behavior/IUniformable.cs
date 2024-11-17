using Godot;
using PostProcessing.Abstractions;

namespace PostProcessing.Behavior
{
	public interface IUniformable
	{
		public Rid GetUniformableRid();
		public ComputeShaderUniform CreateUniform(ComputeShader shader, int slot);
	}
}
