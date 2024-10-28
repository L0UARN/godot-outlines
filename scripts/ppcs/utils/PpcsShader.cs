using System.Collections.Generic;
using Godot;

namespace Outlines.Ppcs.Utils
{
	public class PpcsShader
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
				if (this._ShaderPath.Equals(value))
				{
					return;
				}

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

		private Dictionary<int, Rid> _Uniforms = new();

		public void BindUniform(IPpcsUniformable uniformable, int slot)
		{
			if (this._Uniforms.ContainsKey(slot))
			{
				Rid toCleanup = this._Uniforms[slot];

				if (toCleanup.IsValid && this._Rd.UniformSetIsValid(toCleanup))
				{
					this._Rd.FreeRid(toCleanup);
				}
			}

			Rid uniformRid = uniformable.CreateUniform(this, slot);
			this._Uniforms[slot] = uniformRid;
		}

		public void Run()
		{
			// TODO (or TOREDO)
		}

		public void Cleanup()
		{
			foreach (KeyValuePair<int, Rid> uniform in this._Uniforms)
			{
				if (uniform.Value.IsValid && this._Rd.UniformSetIsValid(uniform.Value))
				{
					this._Rd.FreeRid(uniform.Value);
				}
			}

			this._Uniforms.Clear();

			if (this._PipelineRid.IsValid && this._Rd.ComputePipelineIsValid(this._PipelineRid))
			{
				this._Rd.FreeRid(this._PipelineRid);
				this._PipelineRid = new();
			}

			if (this.ShaderPath != null)
			{
				PpcsShaderPool.ReleaseShader(this._Rd, this._ShaderPath);
				this.Rid = new();
			}
		}
	}
}
