using Godot;

namespace PostProcessingComputeShaders
{
	public class PpcsShader
	{
		protected RenderingDevice _Rd = null;

		protected Rid _Rid = new();
		public Rid Rid
		{
			get => this._Rid;
		}

		protected PpcsUniformImage _InputImageUniform = null;
		protected PpcsImage _InputImage = null;
		public PpcsImage InputImage
		{
			get => this._InputImage;
			set
			{
				if (value.Rid == this._InputImage.Rid)
				{
					return;
				}

				if (this._InputImageUniform == null)
				{
					this._InputImageUniform = new(this._Rd, this, 0, value);
				}
				else
				{
					this._InputImageUniform.Image = value;
				}

				this._InputImage = value;
			}
		}

		protected PpcsUniformImage _OutputImageUniform = null;
		protected PpcsImage _OutputImage = null;
		public PpcsImage OutputImage
		{
			get => _OutputImage;
			set
			{
				if (value.Rid == this._OutputImage.Rid)
				{
					return;
				}

				if (this._OutputImageUniform == null)
				{
					this._OutputImageUniform = new(this._Rd, this, 0, value);
				}
				else
				{
					this._OutputImageUniform.Image = value;
				}

				this._OutputImage = value;
			}
		}

		public PpcsShader(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void Cleanup()
		{
			if (!this._Rid.IsValid)
			{
				return;
			}

			this._InputImageUniform.Cleanup();
			this._OutputImageUniform.Cleanup();
			this._Rd.FreeRid(this._Rid);
		}
	}
}
