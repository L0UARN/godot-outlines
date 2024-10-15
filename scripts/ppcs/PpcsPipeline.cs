using Godot;
using Godot.Collections;

namespace PostProcessingComputeShaders
{
	public class PpcsPipeline
	{
		protected RenderingDevice _Rd = null;
		protected Array<PpcsShader> _Steps = new();
		protected PpcsImage _InputImage = null;
		protected PpcsImage _BufferImage1 = null;
		protected PpcsImage _BufferImage2 = null;
		protected PpcsImage _CurrentInputImage = null;
		protected PpcsImage _CurrentOutputImage = null;

		public PpcsPipeline(RenderingDevice renderingDevice)
		{
			this._Rd = renderingDevice;
			this._BufferImage1 = new(this._Rd, this._InputImage.Size);
			this._BufferImage2 = new(this._Rd, this._InputImage.Size);
		}

		public void AddStep(PpcsShader newStepToAdd)
		{
			this._Steps.Add(newStepToAdd);
		}

		private void CycleBufferImages(int currentStep)
		{
			if (this._Steps.Count == 1)
			{
				this._CurrentInputImage = this._InputImage;
				this._CurrentOutputImage = this._InputImage;
			}
			else if (currentStep == 0)
			{
				this._CurrentInputImage = this._InputImage;
				this._CurrentOutputImage = this._BufferImage1;
			}
			else if (currentStep == this._Steps.Count - 1)
			{
				this._CurrentInputImage = this._CurrentOutputImage;
				this._CurrentOutputImage = this._InputImage;
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
		}
	}
}
