using Godot;

namespace Outlines
{
	[GlobalClass]
	public partial class OutlinesDisplayDemoComponent : Node
	{
		[Export]
		private OutlinesDisplayComponent _OutlinesDisplayComponent = null;

		private bool _CameraSwitch = false;
		[Export]
		private Camera3D _Camera1 = null;
		[Export]
		private Camera3D _Camera2 = null;

		private void SwitchCamera()
		{
			Camera3D newCamera = this._CameraSwitch ? this._Camera1 : this._Camera2;
			this._OutlinesDisplayComponent.Camera = newCamera;
			newCamera.MakeCurrent();
			this._CameraSwitch = !this._CameraSwitch;
		}

		private bool _EffectSwitch = false;
		[Export]
		private int _OutlinesSize1 = 4;
		[Export]
		private int _GlowRadius1 = 0;
		[Export]
		private int _OutlinesSize2 = 8;
		[Export]
		private int _GlowRadius2 = 8;

		private void SwitchEffect()
		{
			int newOutlinesSize = this._EffectSwitch ? this._OutlinesSize1 : this._OutlinesSize2;
			this._OutlinesDisplayComponent.OutlinesSize = newOutlinesSize;
			int newGlowRadius = this._EffectSwitch ? this._GlowRadius1 : this._GlowRadius2;
			this._OutlinesDisplayComponent.GlowRadius = newGlowRadius;
			this._EffectSwitch = !this._EffectSwitch;
		}

		public override void _Input(InputEvent inputEvent)
		{
			base._Input(inputEvent);

			if (inputEvent.IsAction("SwitchCamera") && inputEvent.IsPressed())
			{
				this.SwitchCamera();
			}
			else if (inputEvent.IsAction("SwitchEffect") && inputEvent.IsPressed())
			{
				this.SwitchEffect();
			}
		}
	}
}
