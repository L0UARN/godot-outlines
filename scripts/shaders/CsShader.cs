using System.Collections.Generic;
using Godot;

namespace GodotOutlines
{
	public class CsShader
	{
		private readonly RenderingDevice _Rd;
		private List<CsUniform> _Uniforms;

		public CsShader(RenderingDevice renderingDevice)
		{
			_Rd = renderingDevice;
			_Uniforms = new();
		}

		public CsShader(RenderingDevice renderingDevice, string shaderPath)
		{
			_Rd = renderingDevice;
			ShaderPath = shaderPath;
			_Uniforms = new();
		}

		private Rid _ShaderRid;
		public Rid ShaderRid
		{
			get => _ShaderRid;
		}

		private Rid _PipelineRid;
		public Rid PipelineRid
		{
			get => _PipelineRid;
		}

		private string _ShaderPath;
		public string ShaderPath
		{
			get => _ShaderPath;
			set
			{
				Cleanup();

				RDShaderFile shaderFile = GD.Load<RDShaderFile>(value);
				RDShaderSpirV shaderSpirV = shaderFile.GetSpirV();

				if (shaderSpirV.CompileErrorCompute.Length > 0)
				{
					GD.PrintErr(shaderSpirV.CompileErrorCompute);
					return;
				}

				_ShaderRid = _Rd.ShaderCreateFromSpirV(shaderSpirV);
				_PipelineRid = _Rd.ComputePipelineCreate(_ShaderRid);
				_ShaderPath = value;
			}
		}

		public void BindUniform(CsUniform uniform)
		{
			_Uniforms.Add(uniform);
		}

		public void ClearUniforms()
		{
			_Uniforms.Clear();
		}

		public void Compute(Vector3I size)
		{
			long computeList = _Rd.ComputeListBegin();
			_Rd.ComputeListBindComputePipeline(computeList, _PipelineRid);

			foreach (CsUniform uniform in _Uniforms)
			{
				_Rd.ComputeListBindUniformSet(computeList, uniform.UniformSetRid, (uint)uniform.Set);
			}

			_Rd.ComputeListDispatch(computeList, (uint)size.X, (uint)size.Y, (uint)size.Z);
			_Rd.ComputeListEnd();
		}

		public void Cleanup()
		{
			if (_ShaderRid.IsValid && _ShaderRid.Id > 0)
			{
				_Rd.FreeRid(_ShaderRid);
				_PipelineRid = new();
			}

			if (_PipelineRid.IsValid && _PipelineRid.Id > 0)
			{
				_Rd.FreeRid(_PipelineRid);
				_PipelineRid = new();
			}
		}
	}
}
