#include "load_data.h"
#include <string.h>

#include "core.h"
#include "graphics.h"
#include "loading.h"
#include "math.h"
#include "physics.h"

#define FIXED2BLOCK(n) ((n) >> (ACC + BLOCK_SHIFT))
#define BLOCK2FIXED(n) ((n) << (ACC + BLOCK_SHIFT))

#define INT2BLOCK(n) ((n) >> (BLOCK_SHIFT))
#define BLOCK2INT(n) ((n) << (BLOCK_SHIFT))

#define FIXED2TILE(n) ((n) >> (ACC + 3))
#define TILE2FIXED(n) ((n) << (ACC + 3))

#define INT2TILE(n) ((n) >> 3)
#define TILE2INT(n) ((n) << 3)

#define VIS_BLOCK_POS(x, y) (((x)&0x1F) + (((y)&0x1F) << 5))

#define X_TILE_BUFFER (1 * BLOCK_SIZE)
#define Y_TILE_BUFFER (1 * BLOCK_SIZE)

#define BLOCK_X 30
#define BLOCK_Y 20

#define BGOFS ((vu16*)(REG_BASE + 0x0010))

#define TILE_INFO	   ((unsigned short*)0x02020000)
#define LEVEL_POINTERS ((unsigned char**)0x0201F000)
#define LOADED_LEVEL   ((unsigned short*)0x02030000)

// the char array in rom of the current level being loaded
unsigned char* level_rom;
// the short array where the level is currently loaded to in ram
unsigned short* level_ram;
// the start of the current level
unsigned short* tileset_data;
// Array of entities to prevent reloading
#define unloaded_len 128
short unloaded_entities[128];
int unload_index;

char level_meta[128];

// unsigned short test_values[256];

int level_loading;

int lvl_width, lvl_height;

#ifdef __DEBUG__
int current_level_index;
#endif

extern int cam_x, cam_y, prev_cam_x, prev_cam_y;

extern int foreground_count;

extern Routine loading_routine;

void load_level_pack(unsigned int* level_pack) {

	for (int i = 0; i < unloaded_len; i++) {
		unloaded_entities[i] = -1;
	}

	int data		  = level_pack[0];
	int index		  = 0;
	int level_loading = 0;

	LEVEL_POINTERS[level_loading++] = data;
	level_pack++;

	data = level_pack[0];

	while (data) {
		switch (data & 0xF) {
			case 1: // Set up for next level

				LEVEL_POINTERS[level_loading++] = (unsigned char*)level_pack[1];

				level_pack++;
				break;
			case 4: // Load in tileset collision data
				{
					int i;

					level_pack++;
					for (i = 0; i < level_pack[i] < 0x0FFFFFFF; ++i) {
						tile_types[i] = level_pack[i];
						level_pack++;
					}

					break;
				}
		}

		level_pack++;
		data = level_pack[0];
	}

	REMOVE_ENGINE_FLAG(LOADING_ASYNC);
}

void load_level(int level) {
#ifdef __DEBUG__
	current_level_index = level;
#endif

	level_loading = level;
	level_rom	  = LEVEL_POINTERS[level];
	load_level_code();
}

void load_level_code() {

	lvl_width  = ((short*)level_rom)[0];
	lvl_height = ((short*)level_rom)[1];

	level_rom += 4;

	// Clear level metadata
	int index;
	for (index = 0; index < 128; ++index) {
		level_meta[index] = 0;
	}

	// Set level metadata
	while (level_rom[0] != 0xFF) {
		index = level_rom[0];

		level_meta[(index & 0xFF)] = level_rom[1];
		level_rom += 2;
	}
	level_rom++;

	// Load tilesets
	short* dst	 = LOADED_LEVEL;
	tileset_data = LOADED_LEVEL;

	for (index = 0; index < foreground_count; ++index) {

		// Aligning rom pointer to 4 byte interval
		level_rom += level_rom[0];

		int size = level_rom[0] | (level_rom[1] << 8);

		level_rom += 2;

		LZ77UnCompWram(level_rom, dst);

		level_rom += size;

		dst += 0x2000;
	}

	// unload entities

	index		 = 0;
	max_entities = 0;
	for (; index < ENTITY_LIMIT; ++index) {
		if (ENT_FLAG(PERSISTENT, index)) {
			if (max_entities != index)
				entities[max_entities] = entities[index];

			++max_entities;
		} else {
			entities[index].flags[0] = 0;
			entities[index].flags[1] = 0;
			entities[index].flags[2] = 0;
			entities[index].flags[3] = 0;
			entities[index].flags[4] = 0;
		}
	}

	// int val = tileset_data;

	// val += lvl_width * lvl_height * 2 * foreground_count;
	// val = (val + 3) & ~0x3;

	// level_rom = *((unsigned int**)((unsigned int*)val));

	// load entities
	int type;
	type = *level_rom;
	level_rom++;

	index = 0;

	while (type != 0xFF && max_entities < ENTITY_LIMIT) {
		int x = level_rom[0],
			y = level_rom[1];
		level_rom += 2;

		int ent_idx = (level_loading) | (index << 6);

		for (int i = 0; unloaded_entities[i] != -1; i++) {
			if (unloaded_entities[i] == ent_idx) {
				ent_idx = -1;
				break;
			}
		}

		if (ent_idx >= 0) {
			ent_idx <<= 5;
			int is_loading = add_entity_local(x, y, type, max_entities);

			if (is_loading) {
				entities[max_entities].ID |= ent_idx;
				++max_entities;
			}
		}

		while (*level_rom++ != 0xFF)
			;

		type = level_rom[0];

		index++;
		level_rom++;
	}

	level_rom = NULL;
}

int add_entity_local(int x, int y, int type, int ent) {
	entities[ent].vel_x = 0;
	entities[ent].vel_y = 0;

	entities[ent].x	 = BLOCK2FIXED(x);
	entities[ent].y	 = BLOCK2FIXED(y);
	entities[ent].ID = type;
	entities[ent].ID |= ENT_LOADED_FLAG | ENT_VISIBLE_FLAG | ENT_ACTIVE_FLAG;

	int is_loading = 1;

	if (entity_inits[type])
		entity_inits[type](ent, level_rom, &is_loading);
	else
		entities[ent].ID &= ~ENT_LOADED_FLAG | ENT_ACTIVE_FLAG;

	return is_loading;
}

int add_entity(int x, int y, int type) {
	int retval = -1;

	unsigned char* ptr = level_rom;
	level_rom		   = NULL;

	int index = 0;
	while (index < ENTITY_LIMIT) {
		if (!ENT_FLAG(LOADED, index)) {
			retval = index;
			add_entity_local(x, y, type, index);

			if (max_entities <= index)
				max_entities = index + 1;
			break;
		}
	}

	level_rom = ptr;
	return retval;
}

void unload_entity(Entity* ent) {
	int idx = (ent->ID & (ENT_ID_LEVEL | ENT_ID_INDEX)) >> 5;

	for (int i = 0; i < unloaded_len; i++) {
		if (unloaded_entities[i] == -1) {
			unloaded_entities[i] = idx;
			break;
		}
	}
}

void protect_cam() {

	if (cam_x < X_TILE_BUFFER)
		cam_x = X_TILE_BUFFER;
	if (cam_y < Y_TILE_BUFFER)
		cam_y = Y_TILE_BUFFER;

	if (cam_x + 240 + X_TILE_BUFFER > BLOCK2INT(lvl_width))
		cam_x = BLOCK2INT(lvl_width) - 240 - X_TILE_BUFFER;
	if (cam_y + 160 + Y_TILE_BUFFER > BLOCK2INT(lvl_height))
		cam_y = BLOCK2INT(lvl_height) - 160 - Y_TILE_BUFFER;

	int l;
	for (l = 0; l < 4; ++l) {
		if (LAYER_GET_TYPE(layers[l]) == LStyle_FG) {

			BGOFS[(l << 1) + 0] = cam_x;
			BGOFS[(l << 1) + 1] = cam_y;

		} else {
			BGOFS[(l << 1) + 0] = FIXED_MULT(cam_x, 0x80);
			BGOFS[(l << 1) + 1] = FIXED_MULT(cam_y, 0x80);
		}
	}
}
#ifdef LARGE_TILES
void copy_tiles(unsigned short* screen, unsigned short* data, int x, int y, int len) {
	int i;

	if (x & 0x1) {
		int vis = data[0];
		if (vis != 0) {
			vis--;

			int tile = TILE_INFO[((vis & 0xFF) << 2) | ((y ^ (vis >> 11)) & 0x1) | (((~vis >> 10) & 0x1) << 1)];

			tile ^= vis & 0x0C00; // Flip Index
			tile += vis & 0xF000; // Palette
			screen[0] = tile;
		}

		len--;
		screen++;
		data++;
	}

	int testIndex = 0;

	for (i = (len & ~0x1) - 2; i >= 0; i -= 2) {
		int vis = data[i >> 1];
		if (vis == 0)
			continue;
		vis--;

		int tile = TILE_INFO[((vis & 0xFF) << 2) | ((y ^ (vis >> 11)) & 0x1) | (((vis >> 10) & 0x1) << 1)];

		tile ^= vis & 0x0C00; // Flip Index
		tile += vis & 0xF000; // Palette

		screen[i] = tile;

		tile = TILE_INFO[((vis & 0xFF) << 2) | ((y ^ (vis >> 11)) & 0x1) | (((~vis >> 10) & 0x1) << 1)];

		tile ^= vis & 0x0C00; // Flip Index
		tile += vis & 0xF000; // Palette
		screen[i + 1] = tile;
	}

	if (len & 0x1) {
		int vis = data[len >> 1];
		if (vis != 0) {
			vis--;

			int tile = TILE_INFO[((vis & 0xFF) << 2) | ((y ^ (vis >> 11)) & 0x1) | (((vis >> 10) & 0x1) << 1)];

			tile ^= vis & 0x0C00; // Flip Index
			tile += vis & 0xF000; // Palette
			screen[len - 1] = tile;
		}
	}
}
void move_cam() {
	cam_x -= 120;
	cam_y -= 80;

	protect_cam();

	if (foreground_count == 0)
		goto skip_loadcam;

	int moveX = INT2TILE(cam_x) - INT2TILE(prev_cam_x),
		moveY = INT2TILE(cam_y) - INT2TILE(prev_cam_y);

	if (!moveX && !moveY)
		goto skip_loadcam;

	int xMin = INT2TILE(SIGNED_MIN(prev_cam_x, cam_x)) - 1;
	int xMax = INT2TILE(SIGNED_MAX(prev_cam_x, cam_x)) + BLOCK_X + 1;
	int yMin = INT2TILE(SIGNED_MIN(prev_cam_y, cam_y)) - 1;
	int yMax = INT2TILE(SIGNED_MAX(prev_cam_y, cam_y)) + BLOCK_Y + 1;

	unsigned short* foreground = se_mem[31];
	unsigned short* midground  = se_mem[30];
	unsigned short* background = se_mem[29];

	if (foreground_count <= 2)
		background = NULL;
	if (foreground_count <= 1)
		midground = NULL;

	int position;

	// Get the start X and Y rows needed to edit, and the direction each is needed to move.
	// Get the end destinations for x and y.

	int dirX = INT_SIGN(moveX), dirY = INT_SIGN(moveY);
	int startX = (dirX < 0) ? xMin : xMax, startY = (dirY < 0) ? yMin : yMax;
	int endX = startX + moveX, endY = startY + moveY;
	int min, max;

	if (startX != endX)
		startX -= dirX;
	if (startY != endY)
		startY -= dirY;

	if (dirX == 0)
		dirX = 1;
	if (dirY == 0)
		dirY = 1;

	do {

		if (startX != endX) {
			min = startY - (BLOCK_Y + 2) * dirY;
			max = startY;

			for (; min != max; min += dirY) {
				position = VIS_BLOCK_POS(startX, min);

				int vis = tileset_data[(startX >> 1) + ((min >> 1) * lvl_width)];

				int tile = TILE_INFO[((vis & 0xFF) << 2) | ((min ^ (vis >> 11)) & 0x1) | (((startX ^ (vis >> 10)) & 0x1) << 1)];
				tile ^= vis & 0x0C00;
				tile += vis & 0xF000;
				foreground[position] = tile;

				if (midground) {
					tile = TILE_INFO[((vis & 0xFF) << 2) | ((min ^ (vis >> 11)) & 0x1) | (((startX ^ (vis >> 10)) & 0x1) << 1)];
					tile ^= vis & 0x0C00;
					tile += vis & 0xF000;
					midground[position] = tile;
				}
				if (background) {
					tile = TILE_INFO[((vis & 0xFF) << 2) | ((min ^ (vis >> 11)) & 0x1) | (((startX ^ (vis >> 10)) & 0x1) << 1)];
					tile ^= vis & 0x0C00;
					tile += vis & 0xF000;
					background[position] = tile;
				}
			}
			startX += dirX;
		}

		if (startY != endY) {

			min = startX - (BLOCK_X + 2) * dirX;
			max = startX;

			for (; min != max; min += dirX) {
				position = VIS_BLOCK_POS(min, startY);

				int vis = tileset_data[(min >> 1) + ((startY >> 1) * lvl_width)];

				int tile = TILE_INFO[((vis & 0xFF) << 2) | ((startY ^ (vis >> 11)) & 0x1) | (((min ^ (vis >> 10)) & 0x1) << 1)];
				tile ^= vis & 0x0C00;
				tile += vis & 0xF000;
				foreground[position] = tile;

				if (midground) {
					tile = TILE_INFO[((vis & 0xFF) << 2) | ((startY ^ (vis >> 11)) & 0x1) | (((min ^ (vis >> 10)) & 0x1) << 1)];
					tile ^= vis & 0x0C00;
					tile += vis & 0xF000;
					midground[position] = tile;
				}
				if (background) {
					tile = TILE_INFO[((vis & 0xFF) << 2) | ((startY ^ (vis >> 11)) & 0x1) | (((min ^ (vis >> 10)) & 0x1) << 1)];
					tile ^= vis & 0x0C00;
					tile += vis & 0xF000;
					background[position] = tile;
				}
			}
			startY += dirY;
		}
	} while (startX != endX || startY != endY);

skip_loadcam:

	prev_cam_x = cam_x;
	prev_cam_y = cam_y;

	cam_x += 120;
	cam_y += 80;
}
void reset_cam() {
	// Get top left position
	cam_x -= 120;
	cam_y -= 80;

	protect_cam();

	// If there are no layers to set, don't change anything
	if (foreground_count == 0)
		goto skip_loadcam;

	int val;

	int x = INT2TILE(cam_x);
	int y = INT2TILE(cam_y);

	// x &= ~0x1;

	unsigned short* foreground = se_mem[31];
	unsigned short* midground  = se_mem[30];
	unsigned short* background = se_mem[29];

	if (foreground_count < 3)
		background = 0;
	if (foreground_count < 2)
		midground = 0;

	val = 22;
	while (val-- > 0) {

		int p1 = VIS_BLOCK_POS(x, y);
		int p2 = ((y >> 1) * lvl_width) + (x >> 1);

		copy_tiles(&foreground[p1], &tileset_data[p2], x, y, 32 - x);

		p1 &= 0xFE0;
		p2 += (32 - x) >> 1;

		copy_tiles(&foreground[p1], &tileset_data[p2], 0, y, x);

		++y;
	}

skip_loadcam:

	prev_cam_x = cam_x;
	prev_cam_y = cam_y;

	cam_x += 120;
	cam_y += 80;
}
#else
void move_cam() {
	cam_x -= 120;
	cam_y -= 80;

	protect_cam();

	if (foreground_count == 0)
		goto skip_loadcam;

	unsigned short* grounds[3] = {NULL, NULL, NULL};

	int j = 0;

	for (int i = 0; i < 4; ++i) {
		if (LAYER_GET_TYPE(layers[i]) == LStyle_FG) {
			grounds[j++] = &se_mem[(layers[i].gba_meta & 0x1F00) >> 8];
		}
	}

	int moveX = INT2BLOCK(cam_x) - INT2BLOCK(prev_cam_x),
		moveY = INT2BLOCK(cam_y) - INT2BLOCK(prev_cam_y);

	if (!moveX && !moveY)
		goto skip_loadcam;

	int xMin = INT2BLOCK(SIGNED_MIN(prev_cam_x, cam_x)) - 1;
	int xMax = INT2BLOCK(SIGNED_MAX(prev_cam_x, cam_x)) + BLOCK_X + 1;
	int yMin = INT2BLOCK(SIGNED_MIN(prev_cam_y, cam_y)) - 1;
	int yMax = INT2BLOCK(SIGNED_MAX(prev_cam_y, cam_y)) + BLOCK_Y + 1;

	int position;

	// Get the start X and Y rows needed to edit, and the direction each is needed to move.
	// Get the end destinations for x and y.

	int dirX = INT_SIGN(moveX), dirY = INT_SIGN(moveY);
	int startX = (dirX < 0) ? xMin : xMax, startY = (dirY < 0) ? yMin : yMax;
	int endX = startX + moveX, endY = startY + moveY;
	int min, max;

	if (startX != endX)
		startX -= dirX;
	if (startY != endY)
		startY -= dirY;

	if (dirX == 0)
		dirX = 1;
	if (dirY == 0)
		dirY = 1;

	do {

		if (startX != endX) {
			min = startY - (BLOCK_Y + 2) * dirY;
			max = startY;

			for (; min != max; min += dirY) {
				position = VIS_BLOCK_POS(startX, min);

				short* ptr = &tileset_data[startX + (min * lvl_width)];

				grounds[0][position] = TILE_INFO[*ptr & 0xFFF] | (*ptr & 0xF000);
				if (grounds[1])
					grounds[1][position] = TILE_INFO[ptr[0x2000] & 0xFFF] | (ptr[0x2000] & 0xF000);
				// if (background)
				// 	background[position] = tileset_data[startX + (min * lvl_width) + 0x4000];
			}
			startX += dirX;
		}

		if (startY != endY) {

			min = startX - (BLOCK_X + 2) * dirX;
			max = startX;

			for (; min != max; min += dirX) {
				position = VIS_BLOCK_POS(min, startY);

				short* ptr = &tileset_data[min + (startY * lvl_width)];

				grounds[0][position] = TILE_INFO[*ptr & 0xFFF] | (*ptr & 0xF000);
				if (grounds[1])
					grounds[1][position] = TILE_INFO[ptr[0x2000] & 0xFFF] | (ptr[0x2000] & 0xF000);
				// if (background)
				// 	background[position] = tileset_data[min + (startY * lvl_width) + 0x4000];
			}
			startY += dirY;
		}
	} while (startX != endX || startY != endY);

skip_loadcam:

	prev_cam_x = cam_x;
	prev_cam_y = cam_y;

	cam_x += 120;
	cam_y += 80;
}
void reset_cam() {
	cam_x -= 120;
	cam_y -= 80;

	protect_cam();

	if (foreground_count == 0)
		goto skip_loadcam;

	int val;

	int x = INT2BLOCK(cam_x) - 1;
	int y = INT2BLOCK(cam_y) - 1;

	x &= ~0x1;

	unsigned short* grounds[3] = {NULL, NULL, NULL};

	int j = 0;

	for (int i = 0; i < 4; ++i) {
		if (LAYER_GET_TYPE(layers[i]) == LStyle_FG) {
			grounds[j++] = &se_mem[(layers[i].gba_meta & 0x1F00) >> 8];
		}
	}

	val = 22;
	while (val-- > 0) {

		int p1 = VIS_BLOCK_POS(x, y);
		int p2 = (y * lvl_width) + x;
		++y;

		int idx = 31 - x;

		for (; idx > 0; idx--) {
			short* ptr			 = &tileset_data[p2 + idx];
			grounds[0][p1 + idx] = TILE_INFO[*ptr & 0xFFF] | (*ptr & 0xF000);

			if (grounds[1])
				grounds[1][p1 + idx] = TILE_INFO[ptr[0x2000] & 0xFFF] | (ptr[0x2000] & 0xF000);
			// if (background)
			// 	background[p1 + idx] = tileset_data[p2 + 0x4000 + idx];
		}

		p1 &= 0xFE0;
		p2 += 32 - x;

		for (idx = x; idx > 0; idx--) {
			short* ptr			 = &tileset_data[p2 + idx];
			grounds[0][p1 + idx] = TILE_INFO[*ptr & 0xFFF] | (*ptr & 0xF000);

			if (grounds[1])
				grounds[1][p1 + idx] = TILE_INFO[ptr[0x2000] & 0xFFF] | (ptr[0x2000] & 0xF000);

			// if (midground)
			// 	midground[p1 + idx] = tileset_data[p2 + 0x2000 + idx];
			// if (background)
			// 	background[p1 + idx] = tileset_data[p2 + 0x4000 + idx];
		}
	}

skip_loadcam:

	prev_cam_x = cam_x;
	prev_cam_y = cam_y;

	cam_x += 120;
	cam_y += 80;
}
#endif