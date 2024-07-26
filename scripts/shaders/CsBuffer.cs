using Godot;

namespace GodotOutlines
{
	public class CsBuffer
	{
		protected readonly RenderingDevice _Rd;

		protected Rid _BufferRid;
		public Rid BufferRid
		{
			get => _BufferRid;
		}

		public CsBuffer(RenderingDevice renderingDevice)
		{
			_Rd = renderingDevice;
		}

		public void Cleanup()
		{
			if (_BufferRid.IsValid && _BufferRid.Id > 0)
			{
				_Rd.FreeRid(_BufferRid);
				_BufferRid = new();
			}
		}
	}
}
