using GTA;
using GTA.Math;
using GTA.Native;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GTAVScript
{
	public class ItemInspector : Script
	{
		public ItemInspector()
		{
			Tick += OnTick;
			KeyDown += OnKeyDown;
			KeyUp += OnKeyUp;

			Logging.SetupLogging();

			UI.Notify("ItemInspector Started");
			Log.Information("ItemInspector Started-------------------------------------");
		}

		private void OnTick(object sender, EventArgs e)
		{
			var weapons = Game.Player.Character.Weapons;
			if (weapons.Current.Hash == WeaponHash.StunGun)
			{
				InspectTarget();
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{

		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{

		}

		private void InspectTarget()
		{
			var raycastResult = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 100, IntersectOptions.Everything, Game.Player.Character);

			if (raycastResult.DitHitAnything)
			{
				var rayTarget = raycastResult.HitCoords;
				var rayNormal = raycastResult.SurfaceNormal;
				var rayNormalEnd = rayTarget + rayNormal;
				var color = Color.Yellow;

				Function.Call(Hash.DRAW_LINE, rayTarget.X, rayTarget.Y, rayTarget.Z, rayNormalEnd.X, rayNormalEnd.Y, rayNormalEnd.Z, color.R, color.G, color.B, color.A);

				var rayNormalPerp = Vector3.Cross(rayNormal, Vector3.WorldUp);
				var rayNormalPerpEnd = rayTarget + rayNormalPerp;
				color = Color.Red;
				Function.Call(Hash.DRAW_LINE, rayTarget.X, rayTarget.Y, rayTarget.Z, rayNormalPerpEnd.X, rayNormalPerpEnd.Y, rayNormalPerpEnd.Z, color.R, color.G, color.B, color.A);

				rayNormalPerp = Vector3.Cross(rayNormal, rayNormalPerp);
				rayNormalPerpEnd = rayTarget + -rayNormalPerp;
				color = Color.Blue;
				Function.Call(Hash.DRAW_LINE, rayTarget.X, rayTarget.Y, rayTarget.Z, rayNormalPerpEnd.X, rayNormalPerpEnd.Y, rayNormalPerpEnd.Z, color.R, color.G, color.B, color.A);
			}
		}
	}
}
