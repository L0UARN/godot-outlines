using Godot;
using Godot.Collections;

namespace Outlines.Ppcs.Utils
{
	public class PpcsUniformBuffer : PpcsUniform
	{
		protected PpcsBuffer _Buffer;
		public PpcsBuffer Buffer
		{
			get => this._Buffer;
			set
			{
				if (value.Rid.Equals(this._Buffer?.Rid))
				{
					return;
				}

				this.Cleanup();

				RDUniform uniform = new()
				{
					UniformType = RenderingDevice.UniformType.StorageBuffer,
					Binding = 0,
				};
				uniform.AddId(value.Rid);

				this.Rid = this._Rd.UniformSetCreate(new Array<RDUniform> { uniform }, this._Shader.Rid, (uint)this.Set);
				this._Buffer = value;
			}
		}

		public PpcsUniformBuffer(RenderingDevice renderingDevice, PpcsShader shader, int set, PpcsBuffer buffer) : base(renderingDevice, shader, set)
		{
			this.Buffer = buffer;
		}
	}
}
