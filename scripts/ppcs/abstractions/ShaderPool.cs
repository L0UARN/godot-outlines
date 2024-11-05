using System;
using System.Collections.Generic;
using Godot;

namespace Ppcs.Abstractions
{
	public static class ShaderPool
	{
		private readonly static Dictionary<StringName, Rid> _ShaderRids = new();
		private readonly static Dictionary<StringName, int> _ShaderReferenceCounts = new();

		public static Rid GetOrCreateShaderRid(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (ShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return ShaderPool._ShaderRids[shaderPath];
			}

			RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
			RDShaderSpirV shaderSpirV = shaderFile.GetSpirV();

			if (shaderSpirV.CompileErrorCompute.Length > 0)
			{
				throw new Exception(shaderSpirV.CompileErrorCompute);
			}

			Rid shaderRid = renderingDevice.ShaderCreateFromSpirV(shaderSpirV);
			ShaderPool._ShaderRids[shaderPath] = shaderRid;
			return shaderRid;
		}

		public static void HoldShader(StringName shaderPath)
		{
			if (!ShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!ShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				ShaderPool._ShaderReferenceCounts[shaderPath] = 1;
				return;
			}

			ShaderPool._ShaderReferenceCounts[shaderPath]++;
		}

		public static void ReleaseShader(RenderingDevice renderingDevice, StringName shaderPath)
		{
			if (!ShaderPool._ShaderRids.ContainsKey(shaderPath))
			{
				return;
			}

			if (!ShaderPool._ShaderReferenceCounts.ContainsKey(shaderPath))
			{
				return;
			}

			ShaderPool._ShaderReferenceCounts[shaderPath]--;

			if (ShaderPool._ShaderReferenceCounts[shaderPath] > 0)
			{
				return;
			}

			if (ShaderPool._ShaderRids[shaderPath].IsValid)
			{
				renderingDevice.FreeRid(ShaderPool._ShaderRids[shaderPath]);
			}

			ShaderPool._ShaderReferenceCounts.Remove(shaderPath);
			ShaderPool._ShaderRids.Remove(shaderPath);
		}
	}
}
