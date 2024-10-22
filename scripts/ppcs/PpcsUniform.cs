using Godot;

namespace Outlines.Ppcs
{
	public class PpcsUniform
	{
		protected RenderingDevice _Rd = null;
		protected PpcsShader _Shader = null;

		public int Set { get; protected set; } = -1;
		public Rid Rid { get; protected set; } = new();

		public PpcsUniform(RenderingDevice renderingDevice, PpcsShader shader, int set)
		{
			this._Rd = renderingDevice;
			this._Shader = shader;
			this.Set = set;
		}

		public void Cleanup()
		{
			if (!this.Rid.IsValid || !this._Rd.UniformSetIsValid(this.Rid))
			{
				return;
			}

			this._Rd.FreeRid(this.Rid);
			this.Rid = new();
		}
	}
}
