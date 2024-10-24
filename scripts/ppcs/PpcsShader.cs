using System.Collections.Generic;
using Godot;

namespace Outlines.Ppcs
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
				if (value.Rid.Equals(this._InputImage?.Rid))
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

				this._InputImage = new(this._Rd, value.Rid);
			}
		}

		protected PpcsUniformImage _OutputImageUniform = null;
		protected PpcsImage _OutputImage = null;
		public PpcsImage OutputImage
		{
			get => _OutputImage;
			set
			{
				if (value.Rid.Equals(this._OutputImage?.Rid))
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

				this._OutputImage = new(this._Rd, value.Rid);
			}
		}

		public List<PpcsUniform> Uniforms { get; protected set; } = new();

		public PpcsShader(RenderingDevice renderingDevice, string shaderPath)
		{
			this._Rd = renderingDevice;
			this._ShaderPath = shaderPath;

			Rid shaderRid = PpcsShaderPool.GetShaderRid(this._Rd, this._ShaderPath);
			PpcsShaderPool.AddShaderReference(this._ShaderPath);
			this._PipelineRid = this._Rd.ComputePipelineCreate(shaderRid);
		}

		public void Run()
		{
			if (this._InputImage == null || this._OutputImage == null)
			{
				return;
			}

			long computeList = this._Rd.ComputeListBegin();
			this._Rd.ComputeListBindComputePipeline(computeList, this._PipelineRid);

			this._Rd.ComputeListBindUniformSet(computeList, this._InputImageUniform.Rid, 0);
			this._Rd.ComputeListBindUniformSet(computeList, this._OutputImageUniform.Rid, 1);

			foreach (PpcsUniform uniform in this.Uniforms)
			{
				this._Rd.ComputeListBindUniformSet(computeList, uniform.Rid, (uint)uniform.Set);
			}

			Vector2I size = new(
				Mathf.FloorToInt(Mathf.Min(this._InputImage.Size.X, this._OutputImage.Size.X) / 8),
				Mathf.FloorToInt(Mathf.Min(this._InputImage.Size.Y, this._OutputImage.Size.Y) / 8)
			);

			this._Rd.ComputeListDispatch(computeList, (uint)size.X, (uint)size.Y, 1);
			this._Rd.ComputeListEnd();
		}

		public void Cleanup()
		{
			this._InputImageUniform?.Cleanup();
			this._OutputImageUniform?.Cleanup();

			if (this._PipelineRid.IsValid && this._Rd.ComputePipelineIsValid(this._PipelineRid))
			{
				this._Rd.FreeRid(this._PipelineRid);
				this._PipelineRid = new();
			}

			PpcsShaderPool.CleanupShader(this._Rd, this._ShaderPath);
		}
	}
}
