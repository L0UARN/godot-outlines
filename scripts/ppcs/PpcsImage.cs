using Godot;

namespace PostProcessingComputeShaders
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
				if (value == this._Size)
				{
					return;
				}

				RDTextureFormat textureFormat = new()
				{
					Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
					Width = (uint)value.X,
					Height = (uint)value.Y,
					UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.CanCopyToBit | RenderingDevice.TextureUsageBits.CanCopyFromBit | RenderingDevice.TextureUsageBits.SamplingBit,
				};

				this._Rid = this._Rd.TextureCreate(textureFormat, new RDTextureView());
				this._Rd.TextureClear(this._Rid, new Color(0.0f, 0.0f, 0.0f, 0.0f), 0, 1, 0, 1);
				this._Size = value;
			}
		}

		protected Rid _Rid = new();
		public Rid Rid
		{
			get => this._Rid;
			set
			{
				if (value == this._Rid)
				{
					return;
				}

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

		public void CopyTo(PpcsImage otherImage)
		{
			Vector3 sizeToCopy = new(Mathf.Min(this._Size.X, otherImage.Size.X), Mathf.Min(this._Size.Y, this._Size.Y), 1);
			_Rd.TextureCopy(this._Rid, otherImage.Rid, Vector3.Zero, Vector3.Zero, sizeToCopy, 0, 0, 0, 0);
		}

		public void Cleanup()
		{
			if (!_Rid.IsValid)
			{
				return;
			}

			_Rd.FreeRid(_Rid);
			_Rid = new();
		}
	}
}
