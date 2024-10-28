using System;
using System.Collections.Generic;
using Godot;

namespace Outlines.Ppcs.Utils
{
	public static class PpcsShaderPool
	{
		private static Dictionary<StringName, Rid> _ShaderRids = new();
		private static Dictionary<StringName, int> _ShaderReferenceCounts = new();

		public static Rid GetOrCreateShaderRid(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (PpcsShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return PpcsShaderPool._ShaderRids[shaderPath];
			}

			RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
			RDShaderSpirV shaderSpirV = shaderFile.GetSpirV();

			if (shaderSpirV.CompileErrorCompute.Length > 0)
			{
				throw new Exception(shaderSpirV.CompileErrorCompute);
			}

			Rid shaderRid = renderingDevice.ShaderCreateFromSpirV(shaderSpirV);
			PpcsShaderPool._ShaderRids[shaderPath] = shaderRid;
			return shaderRid;
		}

		public static void HoldShader(StringName shaderPath)
		{
			if (!PpcsShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!PpcsShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				PpcsShaderPool._ShaderReferenceCounts[shaderPath] = 1;
				return;
			}

			PpcsShaderPool._ShaderReferenceCounts[shaderPath]++;
		}

		public static void ReleaseShader(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (!PpcsShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!PpcsShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				return;
			}

			PpcsShaderPool._ShaderReferenceCounts[shaderPath]--;

			if (PpcsShaderPool._ShaderReferenceCounts[shaderPath] > 0)
			{
				return;
			}

			if (PpcsShaderPool._ShaderRids[shaderPath].IsValid)
			{
				renderingDevice.FreeRid(PpcsShaderPool._ShaderRids[shaderPath]);
			}

			PpcsShaderPool._ShaderReferenceCounts.Remove(shaderPath);
			PpcsShaderPool._ShaderRids.Remove(shaderPath);
		}
	}
}
