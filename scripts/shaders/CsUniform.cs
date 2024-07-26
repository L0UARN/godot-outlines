using Godot;

namespace GodotOutlines
{
	public class CsUniform
	{
		protected readonly RenderingDevice _Rd;
		protected readonly Rid _ShaderRid;

		public CsUniform(RenderingDevice renderingDevice, Rid shaderRid, int set)
		{
			_Rd = renderingDevice;
			_ShaderRid = shaderRid;
			_Set = set;
		}

		protected readonly int _Set;
		public int Set
		{
			get => _Set;
		}

		protected Rid _UniformSetRid;
		public Rid UniformSetRid
		{
			get => _UniformSetRid;
		}

		public void Cleanup()
		{
			if (_UniformSetRid.IsValid && _UniformSetRid.Id > 0)
			{
				_Rd.FreeRid(_UniformSetRid);
				_UniformSetRid = new();
			}
		}
	}
}
