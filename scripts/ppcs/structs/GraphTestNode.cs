using Godot;
using Outlines.Ppcs.Utils;

namespace Outlines.Ppcs.Structs
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
			this._Graph = new(this._Rd);

			PpcsShader jfaInit = new(this._Rd, "res://shaders/jfa_init.glsl");
			PpcsShader jfaStep = new(this._Rd, "res://shaders/jfa_step.glsl");
			PpcsShader jfaOutlines = new(this._Rd, "res://shaders/jfa_outlines.glsl");
			PpcsShader boxBlur1 = new(this._Rd, "res://shaders/box_blur.glsl");
			PpcsShader boxBlur2 = new(this._Rd, "res://shaders/box_blur.glsl");
			PpcsShader composite = new(this._Rd, "res://shaders/composite.glsl");

			this._Graph.AddArc(jfaInit, jfaStep);
			this._Graph.AddArc(jfaStep, jfaOutlines);
			this._Graph.AddArc(jfaOutlines, boxBlur1);
			this._Graph.AddArc(boxBlur1, boxBlur2);
			this._Graph.AddArc(boxBlur2, composite);
			this._Graph.AddArc(jfaOutlines, composite);

			this._Graph.FindStartsAndEnd();
			this._Graph.CheckForCycles();
			this._Graph.CheckForIsolatedNodes();
			this._Graph.CreateBufferPool();
		}
	}
}
