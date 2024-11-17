using System;

namespace Outlines.Tools
{
	public class OutlinesPostProcessingBuilder
	{
		private int _OutlinesSize = 4;
		public int OutlinesSize
		{
			get => this._OutlinesSize;
			set
			{
				if (value < 1)
				{
					throw new Exception("Outlines size can't be less than 1.");
				}

				this._OutlinesSize = value;
			}
		}

		private int _GlowRadius = 2;
		public int GlowRadius
		{
			get => this._GlowRadius;
			set
			{
				if (value < 0)
				{
					throw new Exception("Glow radius can't be negative.");
				}

				this._GlowRadius = value;
			}
		}

		public OutlinesPostProcessingBuilder()
		{
		}

		public OutlinesPostProcessingBuilder(int outlinesSize, int glowRadius)
		{
			this.OutlinesSize = outlinesSize;
			this.GlowRadius = glowRadius;
		}

		public IOutlinesPostProcessing Build()
		{
			IOutlinesPostProcessing result = null;

			if (this._GlowRadius > 0)
			{
				result = new OutlinesPostProcessingGraph(this._OutlinesSize, this._GlowRadius);
			}
			else
			{
				// TODO: build the pipeline
				// result = new OutlinesPostProcessingPipeline(this._OutlinesSize, this._GlowRadius);
			}

			return result;
		}
	}
}
