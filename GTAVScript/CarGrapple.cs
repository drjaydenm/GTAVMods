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
	public class CarGrapple : Script
	{
		int currentRopeHandle = -1;
		bool isWinding = false;
		bool inVehicle = false;

		float rayLength = 20;
		Vector3 rayTarget = Vector3.Zero;
		Vector3 rayNormal = Vector3.Zero;
		bool rayHitValidEntity = false;
		Entity rayEntity = null;
		Vector3 currVehicleTarget = Vector3.Zero;

		Vector3 currVehicleTargetRelative = Vector3.Zero;
		Vector3 hookedEntityTargetRelative = Vector3.Zero;
		Entity currVehicleEntity = null;
		Entity hookedEntity = null;

		public CarGrapple()
		{
			Tick += OnTick;
			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;

			Logging.SetupLogging();

			UI.Notify("CarGrapple Started");
			Log.Information("CarGrapple Started-------------------------------------");
		}

		protected override void Dispose(bool A_0)
		{
			Log.Information("Disposing");
			DetachCurrentVehicle();

			base.Dispose(A_0);
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

			HandleWinding();
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			// In a vehicle and targeting a vehicle
			if (inVehicle && rayHitValidEntity && e.KeyCode == Keys.NumPad1)
			{
				AttachVehicle();
			}

			if (currentRopeHandle != -1 && e.KeyCode == Keys.NumPad3)
			{
				DetachCurrentVehicle();
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

					hookedEntityTargetRelative = rayTarget - rayEntity.Position;
					hookedEntity = rayEntity;

					// Do a backwards raycast to find the hitpoint of the current vehicle
					var backwardsDir = (currentVehicle.Position - rayTarget).Normalized;
					raycastResult = World.Raycast(rayTarget, backwardsDir, rayLength, IntersectOptions.Everything, rayEntity);

					if (raycastResult.DitHitAnything && raycastResult.DitHitEntity)
					{
						currVehicleTarget = raycastResult.HitCoords;
						currVehicleTargetRelative = currVehicleTarget - raycastResult.HitEntity.Position;
						currVehicleEntity = raycastResult.HitEntity;

						return;
					}
					else
					{
						Log.Information("Failed to backwards raycast the current vehicle");
					}
				}
				else
				{
					//Log.Information("Didn't hit a valid entity");
				}
			}
			else
			{
				//Log.Information("Didn't hit anything");
			}

			rayTarget = currentVehicle.Position + (currentVehicle.ForwardVector * rayLength);
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
			// Make sure we get rid of the current rope if it exists
			if (currentRopeHandle != -1)
			{
				DetachCurrentVehicle();
			}

			Log.Information("Attaching Vehicle");

			var currentVehicle = Game.Player.Character.CurrentVehicle;
			var distance = World.GetDistance(currVehicleTarget, rayTarget);

			currentRopeHandle = Function.Call<int>(Hash.ADD_ROPE, currVehicleTarget.X, currVehicleTarget.Y, currVehicleTarget.Z, 0, 0, 0, distance, 2, distance, 0.1, 0, true, true, true, 0, true, 0);
			Log.Information($"Rope Handle {currentRopeHandle} with length {distance}");

			Function.Call(Hash.ATTACH_ENTITIES_TO_ROPE, currentRopeHandle, currentVehicle.Handle, rayEntity.Handle, currVehicleTarget.X, currVehicleTarget.Y, currVehicleTarget.Z, rayTarget.X, rayTarget.Y, rayTarget.Z, distance, true, true, 0, 0);
			Log.Information("Attached entities");

			Function.Call(Hash.ROPE_LOAD_TEXTURES);
			Log.Information("Loaded rope textures");
		}

		private void DetachCurrentVehicle()
		{
			unsafe
			{
				int ropePtr = currentRopeHandle;
				var exists = Function.Call<bool>(Hash.DOES_ROPE_EXIST, &ropePtr);

				if (exists)
				{
					Log.Information("Current rope exists");

					Function.Call(Hash.DELETE_ROPE, &ropePtr);
					Log.Information("Rope deleted");

					currentRopeHandle = -1;
					isWinding = false;
					currVehicleEntity = null;
					hookedEntity = null;
				}
			}
		}

		private void HandleWinding()
		{
			// Calculate rope length
			var currVehicleTargetAbs = currVehicleEntity.Position - currVehicleTargetRelative;
			var hookedEntityTargetAbs = hookedEntity.Position - hookedEntityTargetRelative;
			var length = World.GetDistance(currVehicleTargetAbs, hookedEntityTargetAbs);

			// A rope is currently deployed
			if (currentRopeHandle != -1 && (Game.IsKeyPressed(Keys.NumPad2)))
			{
				if (!isWinding)
				{
					Function.Call(Hash.START_ROPE_WINDING, currentRopeHandle);
					Function.Call(Hash.START_ROPE_UNWINDING_FRONT, currentRopeHandle);
					isWinding = true;
					Log.Information("Start rope winding");
				}
			}
			else
			{
				if (isWinding)
				{
					length = Function.Call<float>(Hash._GET_ROPE_LENGTH, currentRopeHandle);
					Log.Information("Rope length is: " + length);

					// Reset the rope length to the current distance between vehicles
					Function.Call(Hash.ROPE_FORCE_LENGTH, currentRopeHandle, length);
					Function.Call(Hash.ROPE_RESET_LENGTH, currentRopeHandle, length);
					Log.Information("Set rope length to: " + length);

					Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, currentRopeHandle);
					Function.Call(Hash.STOP_ROPE_WINDING, currentRopeHandle);
					isWinding = false;
					Log.Information("Stop rope winding");
				}
			}

			if (Game.IsKeyPressed(Keys.NumPad2))
			{
				// Handle applying force
				Function.Call(Hash.APPLY_FORCE_TO_ENTITY, Game.Player.Character.CurrentVehicle, 3, 0f, 0.5f, 0f, 0f, 0f, 0f, 0, true, true, true, true, true);
				Log.Information("Applying force");
			}
			if (Game.IsKeyPressed(Keys.NumPad8))
			{
				length = Function.Call<float>(Hash._GET_ROPE_LENGTH, currentRopeHandle);
				Log.Information("Rope length is: " + length);

				// Add some extra length
				Function.Call(Hash.ROPE_FORCE_LENGTH, currentRopeHandle, length + 10);
				//Function.Call(Hash.ROPE_RESET_LENGTH, currentRopeHandle, length + 10);

				// Handle applying force
				Function.Call(Hash.APPLY_FORCE_TO_ENTITY, Game.Player.Character.CurrentVehicle, 3, 0f, -0.5f, 0f, 0f, 0f, 0f, 0, true, true, true, true, true);
				Log.Information("Applying force");
			}
		}
	}
}
