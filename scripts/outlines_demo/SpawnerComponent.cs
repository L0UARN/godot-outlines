using System;
using Godot;

namespace OutlinesDemo
{
	[GlobalClass]
	public partial class SpawnerComponent : Node
	{
		[Export]
		public Node3D SpawnPoint { get; set; } = null;
		[Export]
		public Timer SpawnTimer { get; set; } = null;
		[Export]
		public Area3D BoundingBox { get; set; } = null;

		private void Spawn()
		{
			RigidBody3D body = new();
			CollisionShape3D bodyShape = new();
			bodyShape.Shape = new BoxShape3D();
			MeshInstance3D bodyMesh = new();
			bodyMesh.Mesh = new BoxMesh();

			body.AddChild(bodyMesh);
			body.AddChild(bodyShape);
			this.SpawnPoint.AddChild(body);

			body.ApplyImpulse(new Vector3(
				(Random.Shared.NextSingle() - 0.5f) * 2.0f,
				Random.Shared.NextSingle(),
				(Random.Shared.NextSingle() - 0.5f) * 2.0f
			) * 10.0f);
		}

		private void Despawn(Node3D body)
		{
			body.QueueFree();
		}

		public override void _Ready()
		{
			base._Ready();

			if (this.SpawnPoint == null)
			{
				throw new Exception("A spawn point is required.");
			}

			if (this.SpawnTimer == null)
			{
				throw new Exception("A spawn timer is required.");
			}

			if (this.BoundingBox == null)
			{
				throw new Exception("A bounding box is required.");
			}

			this.SpawnTimer.Timeout += Spawn;
			this.BoundingBox.BodyExited += Despawn;
		}
	}
}
