using Ppcs.Abstractions;

namespace Ppcs.Graph
{
	public class GraphBufferBinding
	{
		public Shader Shader { get; private set; } = null;
		public int Slot { get; private set; } = 0;

		public GraphBufferBinding(Shader shader, int slot)
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

			if (obj is GraphBufferBinding other)
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

			return false;
		}

		public override int GetHashCode()
		{
			return System.HashCode.Combine(this.Shader, this.Slot);
		}
	}
}
