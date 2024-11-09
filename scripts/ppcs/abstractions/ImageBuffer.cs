using Godot;
using Godot.Collections;
using Ppcs.Interfaces;

namespace Ppcs.Abstractions
{
	public class ImageBuffer : IUniformable, ICleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private Vector2I _Size = Vector2I.MinValue;
		public Vector2I Size
		{
			get
			{
				if (this._Size.Equals(Vector2I.MinValue))
				{
					RDTextureFormat format = this._Rd.TextureGetFormat(this._Rid);
					this._Size = new((int)format.Width, (int)format.Height);
				}

				return this._Size;
			}
			set
			{
				if (value.Equals(this._Size))
				{
					return;
				}

				this.Cleanup();

				RDTextureFormat textureFormat = new()
				{
					Format = RenderingDevice.DataFormat.R16G16B16A16Unorm,
					Width = (uint)value.X,
					Height = (uint)value.Y,
					UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit,
				};

				this._Rid = this._Rd.TextureCreate(textureFormat, new RDTextureView());
				this._Size = value;
			}
		}

		private Rid _Rid = new();
		public Rid Rid
		{
			get => this._Rid;
			set
			{
				if (value.Equals(this._Rid))
				{
					return;
				}

				this.Cleanup();

				this._Size = Vector2I.MinValue;
				this._Rid = value;
			}
		}

		public ImageBuffer(RenderingDevice renderingDevice, Vector2I size)
		{
			this._Rd = renderingDevice;
			this.Size = size;
		}

		public ImageBuffer(RenderingDevice renderingDevice, Rid rid)
		{
			this._Rd = renderingDevice;
			this.Rid = rid;
		}

		public Rid GetUniformableRid()
		{
			return this._Rid;
		}

		public ComputeShaderUniform CreateUniform(ComputeShader shader, int slot)
		{
			RDUniform uniform = new()
			{
				UniformType = RenderingDevice.UniformType.Image,
				Binding = 0,
			};
			uniform.AddId(this._Rid);

			Rid uniformRid = this._Rd.UniformSetCreate(new Array<RDUniform> { uniform }, shader.Rid, (uint)slot);
			return new(this._Rd, uniformRid, this.GetUniformableRid());
		}

		public void Cleanup()
		{
			if (this._Rid.IsValid && this._Rd.TextureIsValid(this._Rid))
			{
				this._Rd.FreeRid(_Rid);
			}

			this._Rid = new();
			this._Size = Vector2I.Zero;
		}
	}
}
