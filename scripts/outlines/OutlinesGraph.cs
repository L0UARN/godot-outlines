using System.Collections.Generic;
using Ppcs.Graph;
using Ppcs.Abstractions;

namespace Outlines
{
	public class OutlinesGraph : ICleanupable
	{
		private readonly Godot.RenderingDevice _Rd = null;
		private readonly List<ICleanupable> _ToCleanup = new();
		private readonly Graph _Graph = null;
		private Image _Image = null;

		public OutlinesGraph(int outlinesSize, int glowRadius)
		{
			this._Rd = Godot.RenderingServer.GetRenderingDevice();

			if (this._Rd == null)
			{
				return;
			}

			this._Graph = new(this._Rd);
			this._ToCleanup.Add(this._Graph);

			Shader jfaInit = new(this._Rd, "res://assets/shaders/jfa_init.glsl");
			this._ToCleanup.Add(jfaInit);
			this._Graph.CreateArcFromInputToShader(0, jfaInit, 0);

			int stepsNeeded = Godot.Mathf.CeilToInt(System.Math.Log2(outlinesSize));
			Shader lastJfaStep = null;

			for (int i = stepsNeeded - 1; i >= 0; i--)
			{
				Shader nextJfaStep = new(this._Rd, "res://assets/shaders/jfa_step.glsl");
				this._ToCleanup.Add(nextJfaStep);
				Buffer stepSizeBuffer = new(this._Rd, System.BitConverter.GetBytes((int)Godot.Mathf.Pow(2, i)));
				this._ToCleanup.Add(stepSizeBuffer);
				nextJfaStep.BindUniform(stepSizeBuffer, 2);

				if (lastJfaStep == null)
				{
					this._Graph.CreateArcFromShaderToShader(jfaInit, 1, nextJfaStep, 0);
				}
				else
				{
					this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, nextJfaStep, 0);
				}

				lastJfaStep = nextJfaStep;
			}

			Shader jfaOutlines = new(this._Rd, "res://assets/shaders/jfa_outlines.glsl");
			this._ToCleanup.Add(jfaOutlines);
			this._Graph.CreateArcFromShaderToShader(lastJfaStep, 1, jfaOutlines, 0);
			this._Graph.CreateArcFromInputToShader(0, jfaOutlines, 1);
			Buffer outlinesSizeBuffer = new(this._Rd, System.BitConverter.GetBytes(outlinesSize));
			this._ToCleanup.Add(outlinesSizeBuffer);
			jfaOutlines.BindUniform(outlinesSizeBuffer, 3);

			Shader lastShader = jfaOutlines;
			int lastShaderOutputSlot = 2;

			if (glowRadius > 0)
			{
				Buffer blurRadiusBuffer = new(this._Rd, System.BitConverter.GetBytes(glowRadius));
				this._ToCleanup.Add(blurRadiusBuffer);

				Shader boxBlur1 = new(this._Rd, "res://assets/shaders/box_blur.glsl");
				this._ToCleanup.Add(boxBlur1);
				Buffer blurDirectionBuffer1 = new(this._Rd, System.BitConverter.GetBytes(true));
				boxBlur1.BindUniform(blurRadiusBuffer, 2);
				boxBlur1.BindUniform(blurDirectionBuffer1, 3);
				this._Graph.CreateArcFromShaderToShader(jfaOutlines, 2, boxBlur1, 0);

				Shader boxBlur2 = new(this._Rd, "res://assets/shaders/box_blur.glsl");
				this._ToCleanup.Add(boxBlur2);
				Buffer blurDirectionBuffer2 = new(this._Rd, System.BitConverter.GetBytes(false));
				boxBlur2.BindUniform(blurRadiusBuffer, 2);
				boxBlur2.BindUniform(blurDirectionBuffer2, 3);
				this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);

				Shader composite = new(this._Rd, "res://assets/shaders/composite.glsl");
				this._ToCleanup.Add(composite);
				Image temp = new(this._Rd, new Godot.Vector2I(1920, 1080));
				this._ToCleanup.Add(temp);
				composite.BindUniform(temp, 2);
				this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, composite, 0);
				this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);

				lastShader = composite;
				lastShaderOutputSlot = 2;
			}

			this._Graph.CreateArcFromShaderToOutput(lastShader, lastShaderOutputSlot, 0);
		}

		public void Run(Godot.Rid image)
		{
			if (this._Graph == null)
			{
				return;
			}

			if (!this._Graph.IsBuilt() || !image.Equals(this._Image.Rid))
			{
				this._Image = new(this._Rd, image);
				this._Graph.ProcessingSize = this._Image.Size;
				this._Graph.BindInput(0, this._Image);
				this._Graph.BindOutput(0, this._Image);

				if (!this._Graph.IsBuilt())
				{
					this._Graph.Build();
				}
			}

			this._Graph.Run();
		}

		public void Cleanup()
		{
			foreach (ICleanupable cleanupable in this._ToCleanup)
			{
				cleanupable.Cleanup();
			}

			_ToCleanup.Clear();
		}
	}
}
