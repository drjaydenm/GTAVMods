using GTA;
using GTA.Math;
using GTA.Native;
using Serilog;
using Serilog.Sinks.File;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVScript
{
	public class Grapple : Script
	{
		List<Vehicle> ropedVehicles = new List<Vehicle>();
		bool inVehicle = false;

		float rayLength = 10;
		Vector3 rayTarget = Vector3.Zero;
		Vector3 rayNormal = Vector3.Zero;
		bool rayHitValidEntity = false;
		Entity rayEntity = null;
		Vector3 currVehicleTarget = Vector3.Zero;

		public Grapple()
		{
			Tick += OnTick;
			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;

			Logging.SetupLogging();

			UI.Notify("Grapple Started");
			Log.Information("Grapple Started-------------------------------------");
		}

		private void OnTick(object sender, EventArgs e)
		{
			if (Game.Player.Character.CurrentVehicle != null)
			{
				inVehicle = true;
				UpdateVehicleRay();

				var vehicle = Game.Player.Character.CurrentVehicle;
				var start = currVehicleTarget;
				var color = Color.White;
				if (!rayHitValidEntity)
				{
					color = Color.Red;
				}
				Function.Call(Hash.DRAW_LINE, start.X, start.Y, start.Z, rayTarget.X, rayTarget.Y, rayTarget.Z, color.R, color.G, color.B, color.A);

				if (rayHitValidEntity)
				{
					color = Color.Yellow;
					var rayNormalEnd = rayTarget + (rayNormal);
					Function.Call(Hash.DRAW_LINE, rayTarget.X, rayTarget.Y, rayTarget.Z, rayNormalEnd.X, rayNormalEnd.Y, rayNormalEnd.Z, color.R, color.G, color.B, color.A);
				}
			}
			else
			{
				inVehicle = false;
				NotInVehicle();
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (inVehicle && rayHitValidEntity && e.KeyCode == Keys.NumPad1)
			{
				AttachVehicle();
			}

			if (inVehicle && e.KeyCode == Keys.NumPad2)
			{
				UI.Notify("Boost");
				var vehicle = Game.Player.Character.CurrentVehicle;
				vehicle.EnginePowerMultiplier = 1000;
			}

			if (inVehicle && e.KeyCode == Keys.NumPad3)
			{
				foreach (var v in ropedVehicles)
				{
					v.EngineRunning = true;
				}
			}
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			
		}

		private void UpdateVehicleRay()
		{
			var currentVehicle = Game.Player.Character.CurrentVehicle;

			var raycastResult = World.Raycast(currentVehicle.Position, currentVehicle.ForwardVector, rayLength, IntersectOptions.Everything, currentVehicle);
			//Log.Information($"Hit Any: {raycastResult.DitHitAnything} | Hit Entity: {raycastResult.DitHitEntity}");

			if (raycastResult.DitHitAnything)
			{
				if (raycastResult.DitHitEntity)
				{
					//Log.Information($"Hit: X:{raycastResult.HitCoords.X} Y:{raycastResult.HitCoords.Y} Z:{raycastResult.HitCoords.Z}");

					rayTarget = raycastResult.HitCoords;
					rayNormal = raycastResult.SurfaceNormal;
					rayHitValidEntity = true;
					rayEntity = raycastResult.HitEntity;

					// Do a backwards raycast to find the hitpoint of the current vehicle
					var backwardsDir = (currentVehicle.Position - rayTarget).Normalized;
					raycastResult = World.Raycast(rayTarget, backwardsDir, rayLength, IntersectOptions.Everything, rayEntity);

					if (raycastResult.DitHitAnything && raycastResult.DitHitEntity)
					{
						currVehicleTarget = raycastResult.HitCoords;
						return;
					}
					else
					{
						Log.Information("Failed to backwards raycast the current vehicle");
					}
				}
				else
				{
					Log.Information("Didn't hit a valid entity");
				}
			}
			else
			{
				Log.Information("Didn't hit anything");
			}

			rayTarget = currentVehicle.Position + (currentVehicle.ForwardVector * 10f);
			rayNormal = Vector3.Zero;
			rayHitValidEntity = false;
			rayEntity = null;
			currVehicleTarget = currentVehicle.Position;
		}

		private void NotInVehicle()
		{
			rayTarget = Vector3.Zero;
			rayNormal = Vector3.Zero;
			rayHitValidEntity = false;
			rayEntity = null;
			currVehicleTarget = Vector3.Zero;
		}

		private void AttachVehicle()
		{
			Log.Information("Attaching Vehicle");

			var currentVehicle = Game.Player.Character.CurrentVehicle;
			var distance = World.GetDistance(currVehicleTarget, rayTarget);

			var ropeHandle = Function.Call<int>(Hash.ADD_ROPE, currVehicleTarget.X, currVehicleTarget.Y, currVehicleTarget.Z, 0, 0, 0, distance, 2, distance, 0.1, 0, true, true, true, 0, true, 0);
			Log.Information($"Rope Handle {ropeHandle} with length {distance}");

			var rope = new Rope(ropeHandle);

			Function.Call(Hash.ATTACH_ENTITIES_TO_ROPE, ropeHandle, currentVehicle.Handle, rayEntity.Handle, currVehicleTarget.X, currVehicleTarget.Y, currVehicleTarget.Z, rayTarget.X, rayTarget.Y, rayTarget.Z, distance, true, true, 0, 0);
			Log.Information("Attached entities");

			Function.Call(Hash.ROPE_LOAD_TEXTURES);
			Log.Information("Loaded rope textures");

			ropedVehicles.Add(currentVehicle);
		}
	}
}
