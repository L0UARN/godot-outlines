using System.Collections.Generic;
using Godot;

namespace PostProcessingComputeShaders
{
	public class PpcsPipeline
	{
		protected RenderingDevice _Rd = null;
		protected List<PpcsShader> _Steps = new();
		protected PpcsImage _InputImage = null;
		protected PpcsImage _OutputImage = null;
		protected PpcsImage _BufferImage1 = null;
		protected PpcsImage _BufferImage2 = null;
		protected PpcsImage _CurrentInputImage = null;
		protected PpcsImage _CurrentOutputImage = null;

		public PpcsPipeline(RenderingDevice renderingDevice, PpcsImage inputImage, PpcsImage outputImage)
		{
			this._Rd = renderingDevice;
			this._InputImage = inputImage;
			this._OutputImage = outputImage;
			this._BufferImage1 = new(this._Rd, this._InputImage.Size);
			this._BufferImage2 = new(this._Rd, this._InputImage.Size);
		}

		public void AddStep(PpcsShader newStepToAdd)
		{
			this._Steps.Add(newStepToAdd);
		}

		private void CycleBufferImages(int currentStep)
		{
			if (currentStep == 0)
			{
				this._CurrentInputImage = this._InputImage;
				this._CurrentOutputImage = this._BufferImage1;
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

		public void Run()
		{
			for (int i = 0; i < this._Steps.Count; i++)
			{
				CycleBufferImages(i);

				this._Steps[i].InputImage = this._CurrentInputImage;
				this._Steps[i].OutputImage = this._CurrentOutputImage;
			}

			this._CurrentOutputImage.CopyTo(this._OutputImage);
		}

		public void Cleanup(bool cleanupSteps = true)
		{
			this._BufferImage1.Cleanup();
			this._BufferImage2.Cleanup();

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
