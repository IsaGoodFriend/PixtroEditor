#include "tonc_vscode.h"

#include "core.h"
#include "math.h"
#include "physics.h"

#define TILE_TYPE_SHIFT 8
#define TILE_TYPE_MASK	0xFF00
#define TILE_SHAPE_MASK 0x00FF

unsigned short* tile_types = (unsigned short*)0x02022000;

bool (*physics_code[255])(int, int, int, int, int, bool);
bool (*collide_code[255])(int, int, int, int);

extern unsigned int lvl_width, lvl_height;
extern unsigned short* tileset_data;

void load_tiletypes(unsigned short* coll_data) {
	int readValue = *coll_data++;
	int index	  = 0;

	while (readValue != 0xFFFF) {
		tile_types[index++] = readValue;

		readValue = *coll_data++;
	}
}

int get_block(int x, int y) {
	x = (x < 0) ? 0 : x;
	y = (y < 0) ? 0 : y;
	x = (x >= lvl_width) ? lvl_width - 1 : x;
	y = (y >= lvl_height) ? lvl_height - 1 : y;

	return tileset_data[x + (y * lvl_width)] & 0x7FF;
}

unsigned int entity_physics(Entity* ent, int hit_mask) {
	if (ent->width <= 0 || ent->height <= 0) {
		return 0;
	}

	// Get the sign (-/+) of the velocity components
	int sign_x = (ent->vel_x >> 31) | 1, sign_y = (ent->vel_y >> 31) | 1;
	int y_is_pos = -(~(ent->vel_y) >> 31); // If y is positive, equals 1, else 0;
	int y_is_neg = ent->vel_y >> 31;	   // If y is negative, equals -1, else 0;
	int x_is_pos = -(~(ent->vel_x) >> 31); // If x is positive, equals 1, else 0;
	int x_is_neg = ent->vel_x >> 31;	   // If x is negative, equals -1, else 0;

	// Box collision indexes - Tile values;
	int idxX, idxY;

	// int top = FIXED2INT(ent->y),
	// 	bot = top + ent->height,
	// 	lef = FIXED2INT(ent->x),
	// 	rgt = lef + ent->width;

	// Get the start and end of the base collisionbox
	int y_min = FIXED2INT(ent->y) - y_is_neg * (ent->height - 1),
		y_max = FIXED2INT(ent->y) + y_is_pos * (ent->height - 1),
		x_min = FIXED2INT(ent->x) - x_is_neg * (ent->width - 1),
		x_max = FIXED2INT(ent->x) + x_is_pos * (ent->width - 1);

	// Block values that were hit - flag
	int hit_value_x = 0, hit_value_y = 0;

	int offsetX = 0xFFFF, offsetY = 0xFFFF;

	int vel;
	if (!ent->vel_x)
		vel = 0;
	else
		vel = FIXED2INT(ent->vel_x + (sign_x * 0x7F) + 0x80 + x_is_neg);

	// X physics
	for (idxX = INT2BLOCK(x_min); idxX != INT2BLOCK(x_max + vel) + sign_x; idxX += sign_x) {
		for (idxY = INT2BLOCK(y_min); idxY != INT2BLOCK(y_max) + sign_y; idxY += sign_y) {
			int block = get_block(idxX, idxY);
			if (!block) // If the block is air, then ignore
				continue;

			int shape = tile_types[block - 1];						 // The actual collision shape
			int type  = (shape & TILE_TYPE_MASK) >> TILE_TYPE_SHIFT; // the collision type (for enabling/disabling certain collisions)
			int mask  = 1 << (type - 1);							 // The bitmask for the collision type

			if (!type || !(mask & hit_mask)) // Ignore if block is being ignored, or
				continue;

			shape = shape & TILE_SHAPE_MASK;

			int temp_offset = 0xFFFFF;

			// detecting colliison
			switch (shape) {

				case 0:
					temp_offset = (BLOCK2FIXED(idxX - x_is_neg) - INT2FIXED(ent->width * x_is_pos)) - ent->x;
					break;
				default:
					if (physics_code[shape - 1]) {

						temp_offset = physics_code[shape - 1](FIXED2INT(ent->x) - BLOCK2INT(idxX), FIXED2INT(ent->y) - BLOCK2INT(idxY), ent->width, ent->height, vel, false);

						if (temp_offset >= 0) {
							continue;
							temp_offset = BLOCK2FIXED(idxX - x_is_neg) - INT2FIXED(ent->width * x_is_pos) - ent->x;
						} else {
							continue;
						}
					} else {
						continue;
					}
			}

			if (INT_ABS(temp_offset) < INT_ABS(offsetX)) // If new movement is smaller, set collision data.
			{
				// Set offset
				offsetX		= temp_offset;
				hit_value_x = mask;
			} else if (temp_offset == offsetX) {
				hit_value_x |= mask;
			}
		}

		if (hit_value_x) {
			ent->x += offsetX;

			if (ent->vel_x != 0 && sign_x == INT_SIGN((BLOCK2FIXED(idxX) + 0x400) - (ent->x + (ent->width >> 1))))
				ent->vel_x = 0;
			else
				hit_value_x = 0;
			break;
		}
	}
	ent->x += ent->vel_x * !(hit_value_x & hit_mask);

	x_min = FIXED2INT(ent->x) - x_is_neg * (ent->width - 1);
	x_max = FIXED2INT(ent->x) + x_is_pos * (ent->width - 1);
	if (!ent->vel_y)
		vel = 0;
	else
		vel = FIXED2INT(ent->vel_y + (sign_y * 0x7F) + 0x80 + y_is_neg);

	// Y Physics
	for (idxY = INT2BLOCK(y_min); idxY != INT2BLOCK(y_max + vel) + sign_y; idxY += sign_y) {
		for (idxX = INT2BLOCK(x_min); idxX != INT2BLOCK(x_max) + sign_x; idxX += sign_x) {
			int block = get_block(idxX, idxY);
			if (!block)
				continue;

			int shape = tile_types[block - 1];
			int type  = (shape & TILE_TYPE_MASK) >> TILE_TYPE_SHIFT;
			int mask  = 1 << (type - 1);

			if (!type || !(mask & hit_mask)) // If the block is 0 or if the block is not solid, ignore
				continue;

			shape = shape & TILE_SHAPE_MASK;

			int temp_offset = 0xFFFFF;

			// detecting collision
			switch (shape) {

				case 0:
					temp_offset = BLOCK2FIXED(idxY - y_is_neg) - INT2FIXED(ent->height * y_is_pos) - ent->y;
					break;

				default:
					if (physics_code[shape - 1]) {

						temp_offset = physics_code[shape - 1](FIXED2INT(ent->x) - BLOCK2INT(idxX), FIXED2INT(ent->y) - BLOCK2INT(idxY), ent->width, ent->height, vel, true);

						if (temp_offset >= 0) {
							temp_offset = BLOCK2FIXED(idxY - y_is_neg) - INT2FIXED(ent->height * y_is_pos) - ent->y;
						} else {
							continue;
						}
					} else {
						continue;
					}
			}

			if (INT_ABS(temp_offset) < INT_ABS(offsetY)) // If new movement is smaller, set collision data.
			{
				// Set offset
				offsetY		= temp_offset;
				hit_value_y = mask;
			} else if (temp_offset == offsetY) {
				hit_value_y |= mask;
			}
		}

		if (hit_value_y) {
			ent->y += offsetY;

			if (ent->vel_y != 0 && sign_y == INT_SIGN((BLOCK2FIXED(idxY) + 0x400) - (ent->y + (ent->height >> 1))))
				ent->vel_y = 0;
			else
				hit_value_y = 0;
			break;
		}
	}
	ent->y += ent->vel_y * !(hit_value_y & hit_mask);

	return (hit_value_x << 16) | hit_value_y;
}
unsigned int collide_rect(int x, int y, int width, int height, int hit_mask) {
	int y_min = y,
		y_max = y_min + height - 1;

	int x_min = x,
		x_max = x_min + width - 1;

	// Block values that were hit - flag
	int blockValue;
	int hitValue = 0;
	int xCoor, yCoor;

	for (xCoor = INT2BLOCK(x_min); xCoor <= INT2BLOCK(x_max); ++xCoor) {
		for (yCoor = INT2BLOCK(y_min); yCoor <= INT2BLOCK(y_max); ++yCoor) {

			int shape = get_block(xCoor, yCoor);
			if (!shape)
				continue;

			shape	 = tile_types[shape - 1];
			int type = (shape & TILE_TYPE_MASK) >> TILE_TYPE_SHIFT;
			int mask = 1 << (type - 1);

			if (!type || !(mask & hit_mask)) // If the block is 0 or if the block is not solid, ignore
				continue;

			shape = shape & TILE_SHAPE_MASK;

			switch (shape) {

				case 0:
					hitValue |= mask;
					break;

				default:
					if (collide_code[shape - 1] && collide_code[shape - 1](x - BLOCK2INT(xCoor), y - BLOCK2INT(yCoor), width, height)) {
						hitValue |= mask;
					}

					continue;
			}
		}
	}

	return hitValue;
}
int collide_entity(unsigned int index) {
	int i = 0;

	Entity* this = &entities[index], *other;

	int id_LX = FIXED2INT(this->x);
	int id_LY = FIXED2INT(this->y);
	int id_RX = id_LX + this->width - 1;
	int id_RY = id_LY + this->height - 1;
	int iter_LX, iter_LY, iter_RX, iter_RY;

	for (; i < ENTITY_LIMIT; ++i) {
		if (i == index)
			continue;
		if (!ENT_FLAG(ACTIVE, i) || !ENT_FLAG(DETECT, i))
			continue;

		other = &entities[i];

		iter_LX = FIXED2INT(other->x);
		iter_LY = FIXED2INT(other->y);
		iter_RX = iter_LX + other->width - 1;
		iter_RY = iter_LY + other->height - 1;

		if (id_RX < iter_LX || iter_RX < id_LX || id_RY < iter_LY || iter_RY < id_LY)
			continue;

		return i;
	}
	return -1;
}
