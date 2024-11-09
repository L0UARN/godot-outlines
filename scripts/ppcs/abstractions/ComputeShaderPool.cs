using System;
using System.Collections.Generic;
using Godot;

namespace Ppcs.Abstractions
{
	public static class ComputeShaderPool
	{
		private readonly static Dictionary<StringName, Rid> _ShaderRids = new();
		private readonly static Dictionary<StringName, int> _ShaderReferenceCounts = new();

		public static Rid GetOrCreateShaderRid(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (ComputeShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return ComputeShaderPool._ShaderRids[shaderPath];
			}

			RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
			RDShaderSpirV shaderSpirV = shaderFile.GetSpirV();

			if (shaderSpirV.CompileErrorCompute.Length > 0)
			{
				throw new Exception(shaderSpirV.CompileErrorCompute);
			}

			Rid shaderRid = renderingDevice.ShaderCreateFromSpirV(shaderSpirV);
			ComputeShaderPool._ShaderRids[shaderPath] = shaderRid;
			return shaderRid;
		}

		public static void HoldShader(StringName shaderPath)
		{
			if (!ComputeShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!ComputeShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				ComputeShaderPool._ShaderReferenceCounts[shaderPath] = 1;
				return;
			}

			ComputeShaderPool._ShaderReferenceCounts[shaderPath]++;
		}

		public static void ReleaseShader(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (!ComputeShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!ComputeShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				return;
			}

			ComputeShaderPool._ShaderReferenceCounts[shaderPath]--;

			if (ComputeShaderPool._ShaderReferenceCounts[shaderPath] > 0)
			{
				return;
			}

			if (ComputeShaderPool._ShaderRids[shaderPath].IsValid)
			{
				renderingDevice.FreeRid(ComputeShaderPool._ShaderRids[shaderPath]);
			}

			ComputeShaderPool._ShaderReferenceCounts.Remove(shaderPath);
			ComputeShaderPool._ShaderRids.Remove(shaderPath);
		}
	}
}
