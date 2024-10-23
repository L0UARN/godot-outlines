using System.Collections.Generic;
using Godot;

namespace Outlines.Ppcs
{
	public class PpcsPipeline
	{
		protected RenderingDevice _Rd = null;
		protected PpcsImage _BufferImage1 = null;
		protected PpcsImage _BufferImage2 = null;
		protected PpcsImage _CurrentInputImage = null;
		protected PpcsImage _CurrentOutputImage = null;
		public List<PpcsShader> Steps { get; private set; } = new();

		public PpcsPipeline(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		private void UpdateBufferImages(PpcsImage inputImage, PpcsImage outputImage)
		{
			Vector2I imagesSize = new(Mathf.Min(inputImage.Size.X, outputImage.Size.X), Mathf.Min(inputImage.Size.Y, outputImage.Size.Y));

			if (this._BufferImage1 == null)
			{
				this._BufferImage1 = new(this._Rd, imagesSize);
			}
			else if (this._BufferImage1.Size != imagesSize)
			{
				this._BufferImage1.Size = imagesSize;
			}

			if (this._BufferImage2 == null)
			{
				this._BufferImage2 = new(this._Rd, imagesSize);
			}
			else if (this._BufferImage2.Size != imagesSize)
			{
				this._BufferImage2.Size = imagesSize;
			}
		}

		private void CycleBufferImages(PpcsImage inputImage, PpcsImage outputImage, int currentStep)
		{
			if (this.Steps.Count == 1)
			{
				this._CurrentInputImage = inputImage;
				this._CurrentOutputImage = outputImage;
			}
			else if (currentStep == 0)
			{
				this._CurrentInputImage = inputImage;
				this._CurrentOutputImage = this._BufferImage1;
			}
			else if (currentStep == this.Steps.Count - 1)
			{
				this._CurrentInputImage = this._CurrentOutputImage;
				this._CurrentOutputImage = outputImage;
			}
			else if (currentStep % 2 == 1)
			{
				this._CurrentInputImage = this._BufferImage1;
				this._CurrentOutputImage = this._BufferImage2;
			}
			else
			{
				this._CurrentInputImage = this._BufferImage2;
				this._CurrentOutputImage = this._BufferImage1;
			}
		}

		public void Run(PpcsImage inputImage, PpcsImage outputImage)
		{
			this.UpdateBufferImages(inputImage, outputImage);

			for (int i = 0; i < this.Steps.Count; i++)
			{
				this.CycleBufferImages(inputImage, outputImage, i);
				this.Steps[i].InputImage = this._CurrentInputImage;
				this.Steps[i].OutputImage = this._CurrentOutputImage;
				this.Steps[i].Run();
			}
		}

		public void Cleanup()
		{
			this._BufferImage1?.Cleanup();
			this._BufferImage2?.Cleanup();
		}
	}
}
