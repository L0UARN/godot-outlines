using Godot;
using PostProcessing.Behavior;

namespace PostProcessing.Abstractions
{
	public class ComputeShaderUniform : ICleanupable
	{
		private readonly RenderingDevice _Rd = null;
		public Rid Rid { get; private set; } = new();
		public Rid UniformableRid { get; private set; } = new();

		public ComputeShaderUniform(RenderingDevice renderingDevice, Rid uniformRid, Rid uniformableRid)
		{
			this._Rd = renderingDevice;
			this.Rid = uniformRid;
			this.UniformableRid = uniformableRid;
		}

		public void Cleanup()
		{
			if (this.Rid.IsValid && this._Rd.UniformSetIsValid(this.Rid))
			{
				this._Rd.FreeRid(this.Rid);
			}

			this.Rid = new();
			this.UniformableRid = new();
		}
	}
}
