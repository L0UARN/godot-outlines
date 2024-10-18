using Godot;
using Godot.Collections;

namespace Ppcs
{
	public class CsUniformBuffer : CsUniform
	{
		private CsBuffer _Buffer;
		public CsBuffer Buffer
		{
			get => _Buffer;
			set
			{
				Cleanup();

				RDUniform uniform = new()
				{
					UniformType = RenderingDevice.UniformType.StorageBuffer,
					Binding = 0,
				};
				uniform.AddId(value.BufferRid);
				_UniformSetRid = _Rd.UniformSetCreate(new Array<RDUniform> { uniform }, _ShaderRid, (uint)_Set);

				_Buffer = value;
			}
		}

		public CsUniformBuffer(RenderingDevice renderingDevice, Rid shaderRid, int set, CsBuffer buffer) : base(renderingDevice, shaderRid, set)
		{
			Buffer = buffer;
		}
	}
}
