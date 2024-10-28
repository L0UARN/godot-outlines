using Godot;
using Godot.Collections;

namespace Outlines.Ppcs.Utils
{
	public class PpcsBuffer : IPpcsUniformable
	{
		private RenderingDevice _Rd = null;
		public Rid Rid { get; private set; } = new();

		private byte[] _Data = null;
		public byte[] Data
		{
			get => this._Data;
			set
			{
				if (value.Equals(this._Data))
				{
					return;
				}

				if (this.Rid.IsValid)
				{
					this._Rd.BufferUpdate(this.Rid, 0, (uint)value.Length, value);
				}
				else
				{
					this.Rid = this._Rd.StorageBufferCreate((uint)value.Length, value);
				}

				this._Data = value;
			}
		}

		public PpcsBuffer(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public PpcsBuffer(RenderingDevice renderingDevice, byte[] data)
		{
			this._Rd = renderingDevice;
			this.Data = data;
		}

		public Rid CreateUniform(PpcsShader shader, int slot)
		{
			RDUniform uniform = new()
			{
				UniformType = RenderingDevice.UniformType.StorageBuffer,
				Binding = 0,
			};
			uniform.AddId(this.Rid);

			return this._Rd.UniformSetCreate(new Array<RDUniform> { uniform }, shader.Rid, (uint)slot);
		}

		public void Cleanup()
		{
			if (!this.Rid.IsValid)
			{
				return;
			}

			this._Rd.FreeRid(this.Rid);
			this.Rid = new();
		}
	}
}
