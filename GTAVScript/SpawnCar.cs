using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using System.Windows.Forms;
using GTA.Native;
using System.Drawing;
using GTA.Math;

namespace GTAVScript
{
	public class SpawnCar : Script
	{
		private Queue<Vehicle> spawnedVehicles;

		public SpawnCar()
		{
			spawnedVehicles = new Queue<Vehicle>();

			Tick += OnTick;
			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;

			UI.Notify("SpawnCar Started");
		}

		private void OnTick(object sender, EventArgs e)
		{
			var weapons = Game.Player.Character.Weapons;
			if (weapons.Current.Hash == WeaponHash.Pistol50)
			{
				if (Game.Player.Character.IsShooting)
				{
					var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 10f;
					SpawnVehicle(VehicleHash.BobcatXL, position, Game.Player.Character.Heading);

					weapons.Current.AmmoInClip = 12;
				}
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.NumPad0)
			{
				var position = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 10f;
				SpawnVehicle(VehicleHash.Adder, position, Game.Player.Character.Heading);
			}
			if (e.KeyCode == Keys.F10)
			{
				UI.Notify("Cleaned Up");
				CleanUp(0);
			}
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void SpawnVehicle(Model model, Vector3 position, float heading)
		{
			Vehicle vehicle = World.CreateVehicle(model, position, heading);
			vehicle.CanTiresBurst = false;
			vehicle.CustomPrimaryColor = Color.FromArgb(38, 38, 38);
			vehicle.CustomSecondaryColor = Color.DarkOrange;
			vehicle.PlaceOnGround();

			spawnedVehicles.Enqueue(vehicle);
			CleanUp(20);

			vehicle.Velocity = vehicle.ForwardVector * 100f;

			// Destroy the vehicle after timeout
			vehicle.MarkAsNoLongerNeeded();
		}

		private void CleanUp(int numToRemain)
		{
			var lastCar = Game.Player.LastVehicle;
			while (spawnedVehicles.Where(v => v.Handle != lastCar.Handle).Count() > numToRemain)
			{
				var vehicleToRemove = spawnedVehicles.Dequeue();

				// If this is the players vehicle, continue
				if (vehicleToRemove.Handle == lastCar.Handle)
				{
					spawnedVehicles.Enqueue(vehicleToRemove);
					continue;
				}
				
				vehicleToRemove.Delete();
			}
		}
	}
}
