using Godot;
using Godot.Collections;

namespace Outlines.Ppcs.Utils
{
	public class PpcsImage : IPpcsUniformable, IPpcsCleanupable
	{
		private readonly RenderingDevice _Rd = null;

		private Vector2I _Size = Vector2I.Zero;
		public Vector2I Size
		{
			get => this._Size;
			private set
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
			private set
			{
				if (value.Equals(this._Rid))
				{
					return;
				}

				this.Cleanup();

				RDTextureFormat format = this._Rd.TextureGetFormat(value);
				this._Size = new((int)format.Width, (int)format.Height);
				this._Rid = value;
			}
		}

		public PpcsImage(RenderingDevice renderingDevice, Vector2I size)
		{
			this._Rd = renderingDevice;
			this.Size = size;
		}

		public PpcsImage(RenderingDevice renderingDevice, Rid rid)
		{
			this._Rd = renderingDevice;
			this.Rid = rid;
		}

		public Rid GetUniformableRid()
		{
			return this._Rid;
		}

		public PpcsUniform CreateUniform(PpcsShader shader, int slot)
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
