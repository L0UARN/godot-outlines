using System;
using Godot;

namespace Ppcs
{
	public class CsBufferBool : CsBuffer
	{
		private bool _Data;
		public bool Data
		{
			get => _Data;
			set
			{
				if (!_BufferRid.IsValid || _BufferRid.Id <= 0)
				{
					_BufferRid = _Rd.StorageBufferCreate(sizeof(bool), BitConverter.GetBytes(value));
				}
				else
				{
					_Rd.BufferUpdate(_BufferRid, 0, sizeof(bool), BitConverter.GetBytes(value));
				}

				_Data = value;
			}
		}

		public CsBufferBool(RenderingDevice renderingDevice, bool data) : base(renderingDevice)
		{
			Data = data;
		}
	}
}
