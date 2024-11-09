using Godot;
using Ppcs.Interfaces;

namespace Outlines
{
	[GlobalClass]
	public partial class CompositorEffectOutlines : CompositorEffect, ICleanupable
	{
		private int _OutlinesSize = 4;
		[Export]
		public int OutlinesSize
		{
			get => this._OutlinesSize;
			set
			{
				if (Engine.IsEditorHint())
				{
					this._OutlinesSize = value;
					return;
				}

				if (value == this._OutlinesSize)
				{
					return;
				}

				this._OutlinesSize = value;
				// TODO: rebuild the graph accounting for the new outlines size
			}
		}

		private int _GlowRadius = 2;
		[Export(PropertyHint.Range, "0,32,1")]
		public int GlowRadius
		{
			get => this._GlowRadius;
			set
			{
				if (Engine.IsEditorHint())
				{
					this._GlowRadius = value;
					return;
				}

				if (value == this._GlowRadius)
				{
					return;
				}

				this._GlowRadius = value;
				// TODO: rebuild the graph accounting for the new glow radius
			}
		}

		private readonly OutlinesGraph _Graph = null;

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			this._Graph = new(this._OutlinesSize, this._GlowRadius);
		}

		public CompositorEffectOutlines(int outlinesSize, int glowRadius) : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			this._OutlinesSize = outlinesSize;
			this._GlowRadius = glowRadius;
			this._Graph = new(this._OutlinesSize, this._GlowRadius);
		}

		public override void _RenderCallback(int effectCallbackType, RenderData renderData)
		{
			base._RenderCallback(effectCallbackType, renderData);

			RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();

			for (uint i = 0; i < renderSceneBuffers.GetViewCount(); i++)
			{
				Rid rawImage = renderSceneBuffers.GetColorLayer(0);
				this._Graph.Run(rawImage);
			}
		}

		public void Cleanup()
		{
			this._Graph.Cleanup();
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
