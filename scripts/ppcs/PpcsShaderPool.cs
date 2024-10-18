using System.Collections.Generic;
using Godot;

namespace PostProcessingComputeShaders
{
	public static class PpcsShaderPool
	{
		private static Dictionary<string, Rid> _Rids = new();
		private static Dictionary<string, int> _References = new();

		public static Rid GetShaderRid(RenderingDevice renderingDevice, string shaderPath)
		{
			if (PpcsShaderPool._Rids.ContainsKey(shaderPath) && PpcsShaderPool._Rids[shaderPath].IsValid)
			{
				return PpcsShaderPool._Rids[shaderPath];
			}

			RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
			RDShaderSpirV shaderSpirV = shaderFile.GetSpirV();

			if (shaderSpirV.CompileErrorCompute.Length > 0)
			{
				GD.PrintErr(shaderSpirV.CompileErrorCompute);
				return new();
			}

			Rid shaderRid = renderingDevice.ShaderCreateFromSpirV(shaderSpirV);
			PpcsShaderPool._Rids[shaderPath] = shaderRid;
			return shaderRid;
		}

		public static void AddShaderReference(string shaderPath)
		{
			if (PpcsShaderPool._References.ContainsKey(shaderPath))
			{
				PpcsShaderPool._References[shaderPath]++;
				return;
			}

			PpcsShaderPool._References[shaderPath] = 1;
		}

		public static void CleanupShader(RenderingDevice renderingDevice, string shaderPath)
		{
			if (!PpcsShaderPool._Rids.ContainsKey(shaderPath) || !PpcsShaderPool._Rids[shaderPath].IsValid)
			{
				return;
			}

			if (PpcsShaderPool._References.ContainsKey(shaderPath) && PpcsShaderPool._References[shaderPath] > 1)
			{
				PpcsShaderPool._References[shaderPath]--;
				return;
			}

			renderingDevice.FreeRid(PpcsShaderPool._Rids[shaderPath]);
			PpcsShaderPool._Rids.Remove(shaderPath);
			PpcsShaderPool._References.Remove(shaderPath);
		}
	}
}
