using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ppcs.Interfaces;

namespace Ppcs.Abstractions
{
	public class ComputeShader : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;

		public StringName ShaderPath { get; private set; } = null;
		public Rid Rid { get; private set; } = new();
		private Rid _PipelineRid = new();

		public ComputeShader(RenderingDevice renderingDevice, StringName shaderPath)
		{
			this._Rd = renderingDevice;

			this.ShaderPath = shaderPath;
			this.Rid = ComputeShaderPool.GetOrCreateShaderRid(this._Rd, this.ShaderPath);
			ComputeShaderPool.HoldShader(this.ShaderPath);
			this._PipelineRid = this._Rd.ComputePipelineCreate(this.Rid);
		}

		private readonly Dictionary<int, ComputeShaderUniform> _Uniforms = new();

		public void BindUniform(IUniformable uniformable, int slot)
		{
			if (this._Uniforms.ContainsKey(slot))
			{
				ComputeShaderUniform previous = this._Uniforms[slot];

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

		public void Run(Vector2I processingSize)
		{
			long computeList = this._Rd.ComputeListBegin();
			this._Rd.ComputeListBindComputePipeline(computeList, this._PipelineRid);

			foreach (KeyValuePair<int, ComputeShaderUniform> uniform in this._Uniforms)
			{
				this._Rd.ComputeListBindUniformSet(computeList, uniform.Value.Rid, (uint)uniform.Key);
			}

			uint runSizeX = (uint) Mathf.FloorToInt(Mathf.Min(processingSize.X, processingSize.X) / 8);
			uint runSizeY = (uint) Mathf.FloorToInt(Mathf.Min(processingSize.Y, processingSize.Y) / 8);

			this._Rd.ComputeListDispatch(computeList, runSizeX, runSizeY, 1);
			this._Rd.ComputeListEnd();
		}

		public void Cleanup()
		{
			foreach (ComputeShaderUniform uniform in this._Uniforms.Values)
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
				ComputeShaderPool.ReleaseShader(this._Rd, this.ShaderPath);
			}

			this.Rid = new();
			this.ShaderPath = null;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is ComputeShader other)
			{
				if (!other.Rid.Equals(this.Rid))
				{
					return false;
				}

				if (!other._PipelineRid.Equals(this._PipelineRid))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		public override string ToString()
		{
			return this.ShaderPath.ToString().Split("/").Last();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Rid, this._PipelineRid);
		}
	}
}
