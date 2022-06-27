using Microsoft.Xna.Framework;
using Monocle;
using System;
using Pixtro.Emulation;
using System.IO;
using System.Text.RegularExpressions;
using Pixtro.Editor;

namespace Pixtro.Scenes {
	internal class MemoryPeekScene : Scene {

		const int CONSOLE_LINES = 512;
		const int LINE_SPACE = 28;

		static int ConsoleIndex;

		float scrollOffset;


		PhysObj testObject;
		int[,] ground;

		public MemoryPeekScene() : base(new Image(Atlases.EngineGraphics["UI/scenes/memory_peek_icon"])) {
			//Camera.Position = new Vector2(0, CONSOLE_LINES * LINE_SPACE);

			ground = new int[5, 5];
			testObject = new PhysObj() {
				width = 8,
				height = 11,
			};

			testObject.x = BLOCK2FIXED(1) + 0;
			testObject.y = BLOCK2FIXED(1);
			//testObject.velx = 0x10;
			testObject.vely = -0x00;

			for (int i = 0; i < 5; i++)
				ground[i, 4] = 1;

			//ground[2, 0] = 1;
			//ground[2, 1] = 1;
			//ground[2, 2] = 1;
			
		}

		public override void OnResize() {
			base.OnResize();

			Camera.Origin = new Vector2(0, 0);
		}

		public override void Update() {
			base.Update();

			if (VisualBounds.Contains(MInput.Mouse.X, MInput.Mouse.Y)) {
				scrollOffset += MInput.Mouse.WheelDelta / 2;
			}

			if (scrollOffset < 0)
				scrollOffset = 0;

			//testObject.x = BLOCK2FIXED(0);
			//testObject.velx = 0x01;
			//testObject.vely += 0x40;

			//testPhysics(testObject);


			//Camera.Position = new Vector2(0, CONSOLE_LINES * LINE_SPACE - (float)Math.Floor(scrollOffset / LINE_SPACE) * LINE_SPACE);
		}

		void testPhysics(PhysObj ent) {

			int hit_mask = 0x1;

			if (ent.width <= 0 || ent.height <= 0)
				return;

			// Get the sign (-/+) of the velocity components
			int sign_x = (ent.velx >> 31) | 1, sign_y = (ent.vely >> 31) | 1;
			int y_is_pos = -(~(ent.vely) >> 31); // If y is positive, equals 1, else 0;
			int y_is_neg = ent.vely >> 31;       // If y is negative, equals -1, else 0;
			int x_is_pos = -(~(ent.velx) >> 31); // If x is positive, equals 1, else 0;
			int x_is_neg = ent.velx >> 31;       // If x is negative, equals -1, else 0;

			// Box collision indexes - Tile values;

			//int top = FIXED2INT(ent.y),
			//	bot = top + ent.height,
			//	lef = FIXED2INT(ent.x),
			//	rgt = lef + ent.width;

			// Get the start and end of the base collisionbox
			int y_min = FIXED2INT(ent.y) - y_is_neg * (ent.height - 1),
				y_max = FIXED2INT(ent.y) + y_is_pos * (ent.height - 1),
				x_min = FIXED2INT(ent.x) - x_is_neg * (ent.width - 1),
				x_max = FIXED2INT(ent.x) + x_is_pos * (ent.width - 1);

			// Block values that were hit - flag
			int hit_value_x = 0, hit_value_y = 0;

			int offsetX = 0xFFFFFF, offsetY = 0xFFFFFF;

			int vel;
			if (ent.velx == 0)
				vel = 0;
			else
				vel = FIXED2INT(ent.velx + (sign_x * 0x7F) + 0x80 + x_is_neg);

			int idxX, idxY;
			// X physics
			for (idxX = INT2BLOCK(x_min); idxX != INT2BLOCK(x_max + vel) + sign_x; idxX += sign_x) {
				for (idxY = INT2BLOCK(y_min); idxY != INT2BLOCK(y_max) + sign_y; idxY += sign_y) {
					int block = get_block(idxX, idxY);
					if (block == 0) // If the block is air, then ignore
						continue;

					int shape = 0x100;                                       // The actual collision shape
					int type  = (shape & TILE_TYPE_MASK) >> TILE_TYPE_SHIFT; // the collision type (for enabling/disabling certain collisions)
					int mask  = 1 << (type - 1);                             // The bitmask for the collision type

					if (type == 0 || (mask & hit_mask) == 0) // Ignore if block is being ignored, or
						continue;

					shape = shape & TILE_SHAPE_MASK;

					int temp_offset = 0xFFFF;

					// detecting colliison
					switch (shape) {

						case 0:
							temp_offset = (BLOCK2FIXED(idxX - x_is_neg) - INT2FIXED(ent.width * x_is_pos)) - ent.x;
							break;

						default:
							//if (physics_code[shape - 1]) {
							//	temp_offset = physics_code[shape - 1]();
							//}
							continue;
					}

					if (INT_ABS(temp_offset) < INT_ABS(offsetX)) // If new movement is smaller, set collision data.
					{
						// Set offset
						offsetX     = temp_offset;
						hit_value_x = mask;
					}
					else if (temp_offset == offsetX) {
						hit_value_x |= mask;
					}
				}

				if (hit_value_x != 0) {
					ent.x += offsetX;

					if (ent.velx != 0 && sign_x == INT_SIGN((BLOCK2FIXED(idxX) + 0x400) - (ent.x + (ent.width >> 1))))
						ent.velx = 0;
					else
						hit_value_x = 0;
					break;
				}
			}
			if ((hit_value_x & hit_mask) == 0)
				ent.x += ent.velx;

			x_min = FIXED2INT(ent.x) - x_is_neg * (ent.width - 1);
			x_max = FIXED2INT(ent.x) + x_is_pos * (ent.width - 1);
			if (ent.vely == 0)
				vel = 0;
			else
				vel = FIXED2INT(ent.vely + (sign_y * 0x7F) + 0x80);

			// Y Physics
			for (idxY = INT2BLOCK(y_min); idxY != INT2BLOCK(y_max + vel) + sign_y; idxY += sign_y) {
				for (idxX = INT2BLOCK(x_min); idxX != INT2BLOCK(x_max) + sign_x; idxX += sign_x) {
					int block = get_block(idxX, idxY);
					if (block == 0)
						continue;

					int shape = 0x101;
					int type  = (shape & TILE_TYPE_MASK) >> TILE_TYPE_SHIFT;
					int mask  = 1 << (type - 1);

					if (type == 0 || (mask & hit_mask) == 0) // Ignore if block is being ignored, or
						continue;

					shape = shape & TILE_SHAPE_MASK;

					int temp_offset = 0xFFFF;

					// detecting collision
					switch (shape) {

						case 0:
							temp_offset = (BLOCK2FIXED(idxY - y_is_neg) - INT2FIXED(ent.height * y_is_pos)) - ent.y;
							break;

						default:
							temp_offset = 0;

							if (temp_offset >= 0) {
								temp_offset = BLOCK2FIXED(idxY - y_is_neg) - INT2FIXED(ent.height * y_is_pos) - ent.y;
							}
							else {
								continue;
							}
							break;
					}

					if (INT_ABS(temp_offset) < INT_ABS(offsetY)) // If new movement is smaller, set collision data.
					{
						// Set offset
						offsetY     = temp_offset;
						hit_value_y = mask;
					}
					else if (temp_offset == offsetY) {
						hit_value_y |= mask;
					}
				}

				if (hit_value_y != 0) {
					ent.y += offsetY;

					if (ent.vely != 0 && sign_y == INT_SIGN((BLOCK2FIXED(idxY) + 0x400) - (ent.y + (ent.height >> 1))))
						ent.vely = 0;
					else
						hit_value_y = 0;
					break;
				}
			}
			if ((hit_value_y & hit_mask) == 0)
				ent.y += ent.vely;
		}

		int FIXED2INT(int i) {
			return i >> 8;
		}
		int INT2FIXED(int i) {
			return i << 8;
		}
		int BLOCK2INT(int i) {
			return i << 3;
		}
		int INT2BLOCK(int i) {
			return i >> 3;
		}
		int FIXED2BLOCK(int i) {
			return i >> 11;
		}
		int BLOCK2FIXED(int i) {
			return i << 11;
		}

		int INT_ABS(int n) {
			n = ((n) * (((n)>>31) | 1));
			return n;
		}
		int INT_SIGN(int n) {
			n = (((n) != 0 ? 1 : 0) * (((n)>>31) | 1));
			return n;
		}
		int get_block(int x, int y) => ground[x, y];

		class PhysObj {
			public int x, y, width, height;
			public int velx, vely;

			public override string ToString() {

				return $"{x / 256f} ; {y / 256f} ; {velx / 256f} ; {vely / 256f}";
			}
		}

		const int TILE_TYPE_MASK  = 0xFF00;
		const int TILE_TYPE_SHIFT = 8;
		const int TILE_SHAPE_MASK = 0x00FF;


		public override void DrawGraphics() {
			base.DrawGraphics();

			if (EmulationHandler.Communication == null)
				return;


			string[] data = new string[]{
				"game_life",
				"death_total",
				"death_temp",
			};


			for (int i = 0; i < data.Length; ++i) {

				int val = EmulationHandler.Communication.GetIntFromRam(data[i]);

				Draw.Text(val.ToString(), new Vector2(0, i * 20), Color.White);
			}
		}
	}
}
