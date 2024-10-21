using Godot;
using Godot.Collections;

namespace Outlines.Ppcs
{
	public class PpcsUniformImage : PpcsUniform
	{
		protected PpcsImage _Image;
		public PpcsImage Image
		{
			get => this._Image;
			set
			{
				if (value.Rid.Equals(this._Image?.Rid))
				{
					return;
				}

				Cleanup();

				RDUniform uniform = new()
				{
					UniformType = RenderingDevice.UniformType.Image,
					Binding = 0,
				};
				uniform.AddId(value.Rid);

				this.Rid = this._Rd.UniformSetCreate(new Array<RDUniform> { uniform }, this._Shader.Rid, (uint)this.Set);
				this._Image = new(this._Rd, value.Rid);
			}
		}

		public PpcsUniformImage(RenderingDevice renderingDevice, PpcsShader shader, int set, PpcsImage image, bool bindToShader = true) : base(renderingDevice, shader, set, bindToShader)
		{
			this.Image = image;
		}
	}
}
