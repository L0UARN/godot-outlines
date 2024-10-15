using Godot;
using Godot.Collections;

namespace PostProcessingComputeShaders
{
	public class PpcsUniformImage : PpcsUniform
	{
		protected PpcsImage _Image;
		public PpcsImage Image
		{
			get => this._Image;
			set
			{
				if (value == this._Image)
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

				this._Rid = this._Rd.UniformSetCreate(new Array<RDUniform> { uniform }, this._Shader.Rid, (uint)_Set);
				this._Image = value;
			}
		}

		public PpcsUniformImage(RenderingDevice renderingDevice, PpcsShader shader, int set, PpcsImage image) : base(renderingDevice, shader, set)
		{
			this.Image = image;
		}
	}
}
