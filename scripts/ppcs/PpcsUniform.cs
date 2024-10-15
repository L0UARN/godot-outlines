using Godot;

namespace PostProcessingComputeShaders
{
	public class PpcsUniform
	{
		protected RenderingDevice _Rd = null;
		protected PpcsShader _Shader = null;

		protected int _Set = -1;
		public int Set
		{
			get => this._Set;
		}

		protected Rid _Rid = new();
		public Rid Rid
		{
			get => this._Rid;
		}

		public PpcsUniform(RenderingDevice renderingDevice, PpcsShader shader, int set)
		{
			this._Rd = renderingDevice;
			this._Shader = shader;
			this._Set = set;
		}

		public void Cleanup()
		{
			if (!this._Rid.IsValid)
			{
				return;
			}

			this._Rd.FreeRid(this._Rid);
			this._Rid = new();
		}
	}
}
