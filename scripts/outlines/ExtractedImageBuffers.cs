using Godot;

namespace Outlines
{
	public class ExtractedImageBuffers
	{
		public Rid ColorLayer { get; set; } = new();
		public Rid DepthLayer { get; set; } = new();

		public ExtractedImageBuffers()
		{
		}
	}
}
