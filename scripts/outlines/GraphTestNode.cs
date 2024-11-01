using Godot;
using Outlines.Ppcs.Structs;
using Outlines.Ppcs.Utils;

namespace Outlines
{
	[GlobalClass]
	public partial class GraphTestNode : Node
	{
		private RenderingDevice _Rd = null;
		private PpcsGraph _Graph = null;

		public override void _Ready()
		{
			base._Ready();

			this._Rd = RenderingServer.GetRenderingDevice();
			this._Graph = new();

			PpcsShader jfaInit = new(this._Rd, "res://shaders/jfa_init.glsl");
			PpcsShader jfaStep = new(this._Rd, "res://shaders/jfa_step.glsl");
			PpcsShader jfaOutlines = new(this._Rd, "res://shaders/jfa_outlines.glsl");
			PpcsShader boxBlur1 = new(this._Rd, "res://shaders/box_blur.glsl");
			PpcsShader boxBlur2 = new(this._Rd, "res://shaders/box_blur.glsl");
			PpcsShader composite = new(this._Rd, "res://shaders/composite.glsl");

			PpcsImage inputImage = new(this._Rd, new Vector2I(10, 10));
			PpcsImage outputImage = new(this._Rd, new Vector2I(10, 10));

			this._Graph.CreateArcFromInputToShader(inputImage, jfaInit, 0);
			this._Graph.CreateArcFromShaderToShader(jfaInit, 1, jfaStep, 0);
			this._Graph.CreateArcFromShaderToShader(jfaStep, 1, jfaOutlines, 0);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, composite, 0);
			this._Graph.CreateArcFromShaderToShader(jfaOutlines, 1, boxBlur1, 0);
			this._Graph.CreateArcFromShaderToShader(boxBlur1, 1, boxBlur2, 0);
			this._Graph.CreateArcFromShaderToShader(boxBlur2, 1, composite, 1);
			this._Graph.CreateArcFromShaderToOutput(composite, 2, outputImage);
		}
	}
}
