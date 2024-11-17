using System;

namespace PostProcessing.Structures.Pipeline.Internal
{
	public class PipelineShaderInputOutput
	{
		public int InputSlot { get; set; } = 0;
		public int OutputSlot { get; set; } = 1;
		public bool HasInputImageAccess { get; set; } = false;
		public int InputImageSlot { get; set; } = 2;

		public PipelineShaderInputOutput(int inputSlot, int outputSlot)
		{
			this.InputSlot = inputSlot;
			this.OutputSlot = outputSlot;
		}

		public PipelineShaderInputOutput(int inputSlot, int outputSlot, int inputImageSlot) : this(inputSlot, outputSlot)
		{
			this.HasInputImageAccess = true;
			this.InputImageSlot = inputImageSlot;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			if (obj is PipelineShaderInputOutput other)
			{
				if (!other.InputSlot.Equals(this.InputSlot))
				{
					return false;
				}

				if (!other.OutputSlot.Equals(this.OutputSlot))
				{
					return false;
				}

				if (!other.HasInputImageAccess.Equals(this.HasInputImageAccess))
				{
					return false;
				}

				if (!other.InputImageSlot.Equals(this.InputImageSlot))
				{
					return false;
				}

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.InputSlot, this.OutputSlot, this.HasInputImageAccess, this.InputImageSlot);
		}
	}
}
