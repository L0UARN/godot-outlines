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

		private readonly Dictionary<int, PpcsUniform> _Uniforms = new();

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

		public void UnbindUniform(int slot)
		{
			if (!this._Uniforms.ContainsKey(slot))
			{
				return;
			}

			this._Uniforms[slot].Cleanup();
			this._Uniforms.Remove(slot);
		}

		public void Run(Vector2I size)
		{
			long computeList = this._Rd.ComputeListBegin();
			this._Rd.ComputeListBindComputePipeline(computeList, this._PipelineRid);

			foreach (KeyValuePair<int, PpcsUniform> uniform in this._Uniforms)
			{
				this._Rd.ComputeListBindUniformSet(computeList, uniform.Value.Rid, (uint)uniform.Key);
			}

			uint runSizeX = (uint) Mathf.FloorToInt(Mathf.Min(size.X, size.X) / 8);
			uint runSizeY = (uint) Mathf.FloorToInt(Mathf.Min(size.Y, size.Y) / 8);

			this._Rd.ComputeListDispatch(computeList, runSizeX, runSizeY, 1);
			this._Rd.ComputeListEnd();
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

		// TODO: remove this once done debugging
		public override string ToString()
		{
			return this._ShaderPath.ToString().Split("/").Last();
		}
	}
}
