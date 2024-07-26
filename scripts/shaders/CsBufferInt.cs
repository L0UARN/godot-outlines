using System;
using Godot;

namespace GodotOutlines
{
	public class CsBufferInt : CsBuffer
	{
		private int _Data;
		public int Data
		{
			get => _Data;
			set
			{
				if (!_BufferRid.IsValid || _BufferRid.Id <= 0)
				{
					_BufferRid = _Rd.StorageBufferCreate(sizeof(int), BitConverter.GetBytes(value));
				}
				else
				{
					_Rd.BufferUpdate(_BufferRid, 0, sizeof(int), BitConverter.GetBytes(value));
				}

				_Data = value;
			}
		}

		public CsBufferInt(RenderingDevice renderingDevice, int data) : base(renderingDevice)
		{
			Data = data;
		}
	}
}
