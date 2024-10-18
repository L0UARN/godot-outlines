using Godot;

namespace PostProcessingComputeShaders
{
	public class PpcsShader
	{
		protected RenderingDevice _Rd = null;

		protected string _ShaderPath = null;
		protected Rid _PipelineRid = new();

		public Rid Rid
		{
			get => PpcsShaderPool.GetShaderRid(this._Rd, this._ShaderPath);
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

		// TODO: add generic uniforms

		public PpcsShader(RenderingDevice renderingDevice, string shaderPath)
		{
			this._Rd = renderingDevice;
			this._ShaderPath = shaderPath;

			Rid shaderRid = PpcsShaderPool.GetShaderRid(this._Rd, this._ShaderPath);
			PpcsShaderPool.AddShaderReference(this._ShaderPath);
			this._PipelineRid = this._Rd.ComputePipelineCreate(shaderRid);
		}

		public void Cleanup()
		{
			if (!this.Rid.IsValid)
			{
				return;
			}

			this._InputImageUniform.Cleanup();
			this._OutputImageUniform.Cleanup();

			// TODO: cleanup generic uniforms

			this._Rd.FreeRid(this._PipelineRid);
			PpcsShaderPool.CleanupShader(this._Rd, this._ShaderPath);
		}
	}
}
