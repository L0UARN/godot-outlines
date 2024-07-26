using Godot;

namespace GodotOutlines
{
	public class CsTexture
	{
		private readonly RenderingDevice _Rd;

		public CsTexture(RenderingDevice renderingDevice)
		{
			_Rd = renderingDevice;
		}

		public CsTexture(RenderingDevice renderingDevice, Vector2I size)
		{
			_Rd = renderingDevice;
			Size = size;
		}

		private Rid _TextureRid;
		public Rid TextureRid
		{
			get => _TextureRid;
		}

		private Vector2I _Size;
		public Vector2I Size
		{
			get => _Size;
			set
			{
				Cleanup();

				RDTextureFormat textureFormat = new()
				{
					Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
					Width = (uint)value.X,
					Height = (uint)value.Y,
					UsageBits = RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.CanCopyToBit | RenderingDevice.TextureUsageBits.CanCopyFromBit | RenderingDevice.TextureUsageBits.SamplingBit,
				};
				_TextureRid = _Rd.TextureCreate(textureFormat, new RDTextureView());
				_Rd.TextureClear(_TextureRid, new Color(0.0f, 0.0f, 0.0f, 0.0f), 0, 1, 0, 1);

				_Size = value;
			}
		}

		public void Copy(CsTexture sourceTexture)
		{
			Vector3 copySize = new(Mathf.Min(_Size.X, sourceTexture.Size.X), Mathf.Min(_Size.Y, sourceTexture.Size.Y), 0.0f);
			_Rd.TextureCopy(sourceTexture.TextureRid, _TextureRid, Vector3.Zero, Vector3.Zero, copySize, 0, 0, 0, 0);
		}

		public void Copy(Rid sourceTexture, Vector2I sourceSize)
		{
			Vector3 copySize = new(Mathf.Min(_Size.X, sourceSize.X), Mathf.Min(_Size.Y, sourceSize.Y), 0.0f);
			_Rd.TextureCopy(sourceTexture, _TextureRid, Vector3.Zero, Vector3.Zero, copySize, 0, 0, 0, 0);
		}

		public void Cleanup()
		{
			if (_TextureRid.IsValid && _TextureRid.Id > 0)
			{
				_Rd.FreeRid(_TextureRid);
				_TextureRid = new();
			}
		}
	}
}
