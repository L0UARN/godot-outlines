using Godot;
using System.Collections.Generic;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectExtractor : CompositorEffect
	{
		public List<ExtractedImageBuffers> ImageBuffers { get; private set; } = new(1);

		[Signal]
		public delegate void ExtractedEventHandler();

		public CompositorEffectExtractor() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
		}

		public override void _RenderCallback(int effectCallbackType, RenderData renderData)
		{
			base._RenderCallback(effectCallbackType, renderData);

			RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();
			uint viewCount = renderSceneBuffers.GetViewCount();

			while (this.ImageBuffers.Count != viewCount)
			{
				if (this.ImageBuffers.Count < viewCount)
				{
					this.ImageBuffers.Add(new());
				}
				else
				{
					this.ImageBuffers.RemoveAt(this.ImageBuffers.Count - 1);
				}
			}

			for (uint i = 0; i < viewCount; i++)
			{
				ExtractedImageBuffers buffers = ImageBuffers[(int)i];
				buffers.ColorLayer = renderSceneBuffers.GetColorLayer(i);
				buffers.DepthLayer = renderSceneBuffers.GetDepthLayer(i);
			}

			this.EmitSignal(SignalName.Extracted);
		}
	}
}
