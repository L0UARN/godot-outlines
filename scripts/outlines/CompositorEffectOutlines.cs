using System.Collections.Generic;
using Godot;
using Outlines.Tools;
using PostProcessing.Behavior;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect, ICleanupable
	{
		private readonly OutlinesPostProcessingBuilder _EffectBuilder = new();
		private readonly List<IOutlinesPostProcessing> _Effects = new(1);

		public int OutlinesSize
		{
			get => this._EffectBuilder.OutlinesSize;
			set
			{
				if (value == this._EffectBuilder.OutlinesSize)
				{
					return;
				}

				this._EffectBuilder.OutlinesSize = value;

				for (int i = 0; i < this._Effects.Count; i++)
				{
					this._Effects[i].Cleanup();
					this._Effects[i] = this._EffectBuilder.Build();
				}
			}
		}

		public int GlowRadius
		{
			get => this._EffectBuilder.GlowRadius;
			set
			{
				if (value == this._EffectBuilder.GlowRadius)
				{
					return;
				}

				this._EffectBuilder.GlowRadius = value;

				for (int i = 0; i < this._Effects.Count; i++)
				{
					this._Effects[i].Cleanup();
					this._Effects[i] = this._EffectBuilder.Build();
				}
			}
		}

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
		}

		public CompositorEffectOutlines(int outlinesSize, int glowRadius) : this()
		{
			this.OutlinesSize = outlinesSize;
			this.GlowRadius = glowRadius;
		}

		public override void _RenderCallback(int effectCallbackType, RenderData renderData)
		{
			base._RenderCallback(effectCallbackType, renderData);

			RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();
			uint viewCount = renderSceneBuffers.GetViewCount();

			// An effect is needed for each view
			while (this._Effects.Count != viewCount)
			{
				if (this._Effects.Count < viewCount)
				{
					this._Effects.Add(this._EffectBuilder.Build());
				}
				else
				{
					this._Effects.RemoveAt(this._Effects.Count - 1);
				}
			}

			for (uint i = 0; i < viewCount; i++)
			{
				Rid rawImage = renderSceneBuffers.GetColorLayer(i);
				this._Effects[(int)i].Run(rawImage);
			}
		}

		public void Cleanup()
		{
			foreach (IOutlinesPostProcessing graph in this._Effects)
			{
				graph.Cleanup();
			}

			this._Effects.Clear();
		}

		public override void _Notification(int what)
		{
			base._Notification(what);

			if (what != NotificationPredelete)
			{
				return;
			}

			this.Cleanup();
		}
	}
}
