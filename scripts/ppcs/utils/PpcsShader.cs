using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Outlines.Ppcs.Utils
{
	public class PpcsShader : IPpcsCleanupable
	{
		private RenderingDevice _Rd = null;

		public Rid Rid { get; private set; } = new();
		private Rid _PipelineRid = new();

		private StringName _ShaderPath = null;
		public StringName ShaderPath
		{
			get => this._ShaderPath;
			private set
			{
				if (value == null)
				{
					this.Cleanup();
					return;
				}

				if (value.Equals(this._ShaderPath))
				{
					return;
				}

				this.Cleanup();

				this.Rid = PpcsShaderPool.GetOrCreateShaderRid(this._Rd, value);
				PpcsShaderPool.HoldShader(value);
				this._ShaderPath = value;
			}
		}

		public PpcsShader(RenderingDevice renderingDevice, StringName shaderPath)
		{
			this._Rd = renderingDevice;
			this.ShaderPath = shaderPath;
		}

		private Dictionary<int, PpcsUniform> _Uniforms = new();

		public void BindUniform(IPpcsUniformable uniformable, int slot)
		{
			if (this._Uniforms.ContainsKey(slot))
			{
				PpcsUniform previous = this._Uniforms[slot];

				if (previous.UniformableRid.Equals(uniformable.GetUniformableRid()))
				{
					return;
				}

				previous.Cleanup();
			}

			this._Uniforms[slot] = uniformable.CreateUniform(this, slot);
		}

		public void Run()
		{
			// TODO (or TOREDO)
		}

		public void Cleanup()
		{
			foreach (PpcsUniform uniform in this._Uniforms.Values)
			{
				uniform.Cleanup();
			}

			this._Uniforms.Clear();

			if (this._PipelineRid.IsValid && this._Rd.ComputePipelineIsValid(this._PipelineRid))
			{
				this._Rd.FreeRid(this._PipelineRid);
			}

			this._PipelineRid = new();

			if (this.ShaderPath != null)
			{
				PpcsShaderPool.ReleaseShader(this._Rd, this._ShaderPath);
			}

			this.Rid = new();
			this._ShaderPath = null;
		}

		public override string ToString()
		{
			return this._ShaderPath.ToString().Split("/").Last();
		}
	}
}
