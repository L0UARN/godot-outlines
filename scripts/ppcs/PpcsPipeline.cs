using System.Collections.Generic;
using Godot;

namespace Ppcs
{
	public class PpcsPipeline
	{
		protected RenderingDevice _Rd = null;
		protected List<PpcsShader> _Steps = new();
		protected PpcsImage _BufferImage1 = null;
		protected PpcsImage _BufferImage2 = null;
		protected PpcsImage _CurrentInputImage = null;
		protected PpcsImage _CurrentOutputImage = null;

		public PpcsPipeline(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
		}

		public void AddStep(PpcsShader newStepToAdd)
		{
			this._Steps.Add(newStepToAdd);
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
			if (this._Steps.Count == 1)
			{
				this._CurrentInputImage = inputImage;
				this._CurrentOutputImage = outputImage;
			}
			else if (currentStep == 0)
			{
				this._CurrentInputImage = inputImage;
				this._CurrentOutputImage = this._BufferImage1;
			}
			else if (currentStep == this._Steps.Count - 1)
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
			UpdateBufferImages(inputImage, outputImage);

			for (int i = 0; i < this._Steps.Count; i++)
			{
				CycleBufferImages(inputImage, outputImage, i);
				this._Steps[i].InputImage = this._CurrentInputImage;
				this._Steps[i].OutputImage = this._CurrentOutputImage;
				this._Steps[i].Run();
			}
		}

		public void Cleanup(bool cleanupSteps = true)
		{
			this._BufferImage1?.Cleanup();
			this._BufferImage2?.Cleanup();

			if (!cleanupSteps)
			{
				return;
			}

			foreach (PpcsShader step in this._Steps)
			{
				step.Cleanup();
			}
		}
	}
}
