using Godot;

namespace Outlines.Ppcs
{
	public class PpcsImage
	{
		protected RenderingDevice _Rd = null;

		protected Vector2I _Size = Vector2I.Zero;
		public Vector2I Size
		{
			get => this._Size;
			set
			{
				if (value.Equals(this._Size))
				{
					return;
				}

				this.Cleanup();

				RDTextureFormat textureFormat = new()
				{
					Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
					Width = (uint)value.X,
					Height = (uint)value.Y,
					UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.SamplingBit,
				};

				this._Rid = this._Rd.TextureCreate(textureFormat, new RDTextureView());
				this._Size = value;
			}
		}

		protected Rid _Rid = new();
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

		public void Cleanup()
		{
			if (!this._Rid.IsValid || !this._Rd.TextureIsValid(this._Rid))
			{
				return;
			}

			this._Rd.FreeRid(_Rid);
			this._Rid = new();
		}
	}
}
