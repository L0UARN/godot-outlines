using System.Collections.Generic;
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

				for (int i = 0; i < this._Graphs.Count; i++)
				{
					this._Graphs[i].Cleanup();
					this._Graphs[i] = new(value, this._GlowRadius);
				}

				this._OutlinesSize = value;
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

				for (int i = 0; i < this._Graphs.Count; i++)
				{
					this._Graphs[i].Cleanup();
					this._Graphs[i] = new(this._OutlinesSize, value);
				}

				this._GlowRadius = value;
			}
		}

		private readonly List<OutlinesGraph> _Graphs = new(1);

		public CompositorEffectOutlines() : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
		}

		public CompositorEffectOutlines(int outlinesSize, int glowRadius) : base()
		{
			this.EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
			this._OutlinesSize = outlinesSize;
			this._GlowRadius = glowRadius;
		}

		public override void _RenderCallback(int effectCallbackType, RenderData renderData)
		{
			base._RenderCallback(effectCallbackType, renderData);

			RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();
			uint viewCount = renderSceneBuffers.GetViewCount();

			if (this._Graphs.Count < viewCount)
			{
				for (int i = 0; i < viewCount - this._Graphs.Count; i++)
				{
					this._Graphs.Add(new(this._OutlinesSize, this._GlowRadius));
				}
			}
			else if (this._Graphs.Count > viewCount)
			{
				for (int i = (int)viewCount; i != this._Graphs.Count; )
				{
					this._Graphs[i].Cleanup();
					this._Graphs.RemoveAt(i);
				}
			}

			for (uint i = 0; i < viewCount; i++)
			{
				Rid rawImage = renderSceneBuffers.GetColorLayer(i);
				this._Graphs[(int)i].Run(rawImage);
			}
		}

		public void Cleanup()
		{
			foreach (OutlinesGraph graph in this._Graphs)
			{
				graph.Cleanup();
			}

			this._Graphs.Clear();
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
