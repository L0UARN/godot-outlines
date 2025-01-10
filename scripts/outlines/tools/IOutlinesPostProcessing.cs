using Godot;
using PostProcessing.Behavior;

namespace Outlines.Tools
{
	public interface IOutlinesPostProcessing : ICleanupable
	{
		public void Run(Rid outlinesColor, Rid outlinesDepth, Rid mainDepth);
	}
}
