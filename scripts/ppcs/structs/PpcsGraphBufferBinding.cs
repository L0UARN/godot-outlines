using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
{
	public class PpcsGraphBufferBinding
	{
		public PpcsShader Shader { get; private set; } = null;
		public int Slot { get; private set; } = 0;

		public PpcsGraphBufferBinding(PpcsShader shader, int slot)
		{
			this.Shader = shader;
			this.Slot = slot;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is PpcsGraphBufferBinding other)
			{
				if (!other.Slot.Equals(this.Slot))
				{
					return false;
				}

				if (other.Shader?.Equals(this.Shader) != true)
				{
					return false;
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return (this.Shader, this.Slot).GetHashCode();
		}
	}
}
