using Godot;
using Godot.Collections;

namespace Ppcs
{
	public class CsUniformTexture : CsUniform
	{
		private CsTexture _Texture;
		public CsTexture Texture
		{
			get => _Texture;
			set
			{
				Cleanup();

				RDUniform uniform = new()
				{
					UniformType = RenderingDevice.UniformType.Image,
					Binding = 0,
				};
				uniform.AddId(value.TextureRid);
				_UniformSetRid = _Rd.UniformSetCreate(new Array<RDUniform> { uniform }, _ShaderRid, (uint)_Set);

				_Texture = value;
			}
		}

		public CsUniformTexture(RenderingDevice renderingDevice, Rid shaderRid, int set, CsTexture texture) : base(renderingDevice, shaderRid, set)
		{
			Texture = texture;
		}
	}
}
