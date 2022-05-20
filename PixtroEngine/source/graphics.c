#include "tonc_vscode.h"
#include <string.h>

#include "core.h"
#include "graphics.h"
#include "load_data.h"

#define copyTile	32
#define copyPalette 32

#define tileSize 8

// Max of 128 sprites
#define SPRITE_LIMIT 128

#define BANK_LIMIT	   64
#define BANK_MEM_START 0x60

int drawing_flags = DFLAG_CAM_FOLLOW | DFLAG_CAM_BOUNDS;
int cam_x, cam_y, prev_cam_x, prev_cam_y;

unsigned short colorbank[512];

char is_rendering;

#define LAYER_META_INIT(index, t, vis) ((index << LAYER_INDEX_SHIFT) | LAYER_TYPE(t) | LAYER_VISIBLE(vis))

#pragma region Sprites

const int shape2size[12] = {
	copyTile,
	copyTile * 4,
	copyTile * 16,
	copyTile * 64,

	copyTile * 2,
	copyTile * 4,
	copyTile * 8,
	copyTile * 32,

	copyTile * 2,
	copyTile * 4,
	copyTile * 8,
	copyTile * 32,
};
const int shape_width[12] = {
	tileSize,
	tileSize * 2,
	tileSize * 4,
	tileSize * 8,

	tileSize * 2,
	tileSize * 4,
	tileSize * 4,
	tileSize * 8,

	tileSize,
	tileSize,
	tileSize * 2,
	tileSize * 4,
};
const int shape_height[12] = {
	tileSize,
	tileSize * 2,
	tileSize * 4,
	tileSize * 8,

	tileSize,
	tileSize,
	tileSize * 2,
	tileSize * 4,

	tileSize * 2,
	tileSize * 4,
	tileSize * 4,
	tileSize * 8,
};

#define SPRITE_X(n) ((n & ATTR1_X_MASK) << ATTR1_X_SHIFT)
#define SPRITE_Y(n) ((n & ATTR0_Y_MASK) << ATTR0_Y_SHIFT)
int sprite_count, prev_sprite_count;
int affine_count;

#define UNLOADED_SPRITE 0xFF

#define TILE_INFO ((unsigned short*)0x0201E000)

// Sprite bank information
char shapes[BANK_LIMIT];
int indexes[BANK_LIMIT], ordered[BANK_LIMIT];

unsigned int *anim_bank[BANK_LIMIT], anim_meta[BANK_LIMIT], wait_to_load[BANK_LIMIT];

OBJ_ATTR obj_buffer[SPRITE_LIMIT];
OBJ_ATTR* sprite_pointer;
OBJ_AFFINE* obj_aff_buffer = (OBJ_AFFINE*)obj_buffer;

extern void load_tiletypes(unsigned int* coll_data);

int load_sprite(unsigned int* sprite, int shape) {
	int value = 0;

	for (; value < BANK_LIMIT; ++value) {
		if (shapes[value] == UNLOADED_SPRITE) {
			load_sprite_at(sprite, value, shape);
			return value;
		}
	}
	return -1;
}
int load_anim_sprite(unsigned int* sprites, int shape, int frames, int speed) {
	int value = 0;

	for (; value < BANK_LIMIT; ++value) {
		if (indexes[value] == NULL) {
			load_anim_sprite_at(sprites, value, shape, frames, speed);
			return value;
		}
	}
	return -1;
}
void load_sprite_at(unsigned int* sprite, int index, int shape) {
	if (!is_rendering) {
		wait_to_load[index] = ((unsigned int)sprite) | (shape << 28);
		shapes[index]		= 0xF0; // Set to distinct value to prevent rewriting, but not a valid shape value
		return;
	}

	// might move this code elsewhere.  Could be causing our anim bug
	// anim_bank[index] = NULL;

	int bankLoc, size = shape2size[shape];

	// If there's a sprite already loaded here, and it's the same size or bigger, replace it
	if (shapes[index] < 12 && shape2size[shapes[index]] >= size) {
		bankLoc = indexes[index];
	} else {
		int i;
		bankLoc = BANK_MEM_START;

		// Search for an open spot in the sprites
		for (i = 0; i < BANK_LIMIT; ++i) {

			int diff = indexes[ordered[i + 1]] - indexes[ordered[i]];

			if (shapes[ordered[i]] < 12) {
				diff -= shape2size[shapes[ordered[i]]] >> 5;
			}

			if (diff >= size >> 5) {
				bankLoc = indexes[ordered[i]];

				if (shapes[ordered[i]] < 12)
					bankLoc += shape2size[shapes[ordered[i]]] >> 5;

				break;
			}
		}
		indexes[index] = bankLoc & 0x7FFF;

		bool swapping = false;

		// Find index in ordered list
		// Check if out of order:
		// if ind-1 is bigger than ind, move ind backward to sort
		// if ind+1 is smaller than ind, move ind forward to sort

		// I have no clue if this works, and I don't know how to test it, so we'll just pretend this works
		int ind;
		for (ind = BANK_LIMIT; ind >= 0; --ind) {

			if (ordered[ind] == index) {
				break;
			}
		}

		while (ind > 0 && indexes[ordered[ind - 1]] > bankLoc) {
			int temp		 = ordered[ind - 1];
			ordered[ind - 1] = index;
			ordered[ind]	 = temp;

			ind--;
		}
		while (ind < BANK_LIMIT - 1 && indexes[ordered[ind + 1]] < bankLoc) {
			int temp		 = ordered[ind + 1];
			ordered[ind + 1] = index;
			ordered[ind]	 = temp;

			ind--;
		}
	}

	shapes[index] = shape;

	memcpy(&tile_mem[4][bankLoc], sprite, size);
}
void load_anim_sprite_at(unsigned int* sprites, int index, int shape, int frames, int speed) {
	load_sprite_at(sprites, index, shape);

	anim_bank[index] = sprites;

	speed &= 0xF;
	speed = (speed)&0xF;
	if (frames)
		frames = (frames - 1) & 0xF;

	anim_meta[index] = speed;
	anim_meta[index] |= speed << 4;

	anim_meta[index] |= frames << 8;
	anim_meta[index] |= frames << 12;

	anim_meta[index] |= shape << 16;
}

void unload_sprite(int index) {
	shapes[index] = UNLOADED_SPRITE;
}

#ifdef LARGE_TILES
void load_tileset(unsigned int* tiles, unsigned short* mapping, unsigned int* collision, int count, int uvcount) {
	memcpy(&tile_mem[FG_TILESET][1], tiles, count << 7);
	memcpy(TILE_INFO + 1, mapping, uvcount << 3);
	load_tiletypes(collision);
}
#else
void load_tileset(unsigned int* tiles, unsigned short* mapping, unsigned int* collision, int count, int uvcount) {
	memcpy(&tile_mem[FG_TILESET][1], tiles, count << 5);
	memcpy(TILE_INFO + 1, mapping, uvcount << 1);
	load_tiletypes(collision);
}
#endif
void load_obj_pal(unsigned short* pal, int palIndex) {
	memcpy(&pal_obj_mem[palIndex << 4], pal, copyPalette);
	memcpy(&colorbank[(palIndex << 3) + 256], pal, copyPalette);
}
void load_bg_pal(unsigned short* pal, int palIndex) {
	memcpy(&pal_bg_mem[palIndex << 4], pal, copyPalette);
	memcpy(&colorbank[palIndex << 3], pal, copyPalette);
}

void draw_affine_big(AffineMatrix matrix, int sprite, int prio, int pal) {
	if (affine_count == 32)
		return;

	int x = FIXED2INT(matrix.values[2]), y = FIXED2INT(matrix.values[5]);

	if (drawing_flags & DFLAG_CAM_FOLLOW) {
		x -= cam_x - 120;
		y -= cam_y - 80;
	}

	int shape = shapes[sprite];

	if (shape == UNLOADED_SPRITE)
		return;

	x -= shape_width[shape];
	y -= shape_height[shape];

	if (x + (shape_width[shape] << 1) <= 0 || x > 240 ||
		y + (shape_height[shape] << 1) <= 0 || y > 160)
		return;

	obj_set_attr(sprite_pointer,
				 ((shape & 0xC) << 12) | SPRITE_Y(y) | ATTR0_AFF_DBL,
				 ((shape & 0x3) << 14) | SPRITE_X(x) | ATTR1_AFF_ID(affine_count),
				 ATTR2_PALBANK(pal) | ATTR2_PRIO(prio) | (indexes[sprite]));

	int det = FIXED_MULT(matrix.values[0], matrix.values[4]) -
			  FIXED_MULT(matrix.values[1], matrix.values[3]);

	if (det)
		det = FIXED_DIV(0x100, det);

	obj_aff_set((obj_aff_buffer + affine_count),
				FIXED_MULT(matrix.values[4], det),
				FIXED_MULT(matrix.values[3], det),
				FIXED_MULT(matrix.values[1], det),
				FIXED_MULT(matrix.values[0], det));

	++sprite_pointer;
	++sprite_count;
	++affine_count;
}
void draw_affine(AffineMatrix matrix, int sprite, int prio, int pal) {
	if (affine_count == 32)
		return;

	AffineMatrix transform = matrix_multiply(matrix_identity(), matrix);

	int x = FIXED2INT(transform.values[2]), y = FIXED2INT(transform.values[5]);

	if (drawing_flags & DFLAG_CAM_FOLLOW) {
		x -= cam_x - 120;
		y -= cam_y - 80;
	}

	int shape = shapes[sprite];

	if (shape == UNLOADED_SPRITE)
		return;

	x -= shape_width[shape] >> 1;
	y -= shape_height[shape] >> 1;

	if (x + shape_width[shape] <= 0 || x > 240 ||
		y + shape_height[shape] <= 0 || y > 160)
		return;

	obj_set_attr(sprite_pointer,
				 ((shape & 0xC) << 12) | SPRITE_Y(y) | ATTR0_AFF,
				 ((shape & 0x3) << 14) | SPRITE_X(x) | ATTR1_AFF_ID(affine_count),
				 ATTR2_PALBANK(pal) | ATTR2_PRIO(prio) | (indexes[sprite]));

	int det = FIXED_MULT(matrix.values[0], matrix.values[4]) -
			  FIXED_MULT(matrix.values[1], matrix.values[3]);

	if (det)
		det = FIXED_DIV(0x100, det);

	obj_aff_set((obj_aff_buffer + affine_count),
				FIXED_MULT(matrix.values[4], det),
				FIXED_MULT(matrix.values[3], det),
				FIXED_MULT(matrix.values[1], det),
				FIXED_MULT(matrix.values[0], det));

	++sprite_pointer;
	++sprite_count;
	++affine_count;
}
void draw(int x, int y, int sprite, int flip, int prio, int pal) {
	x = FIXED2INT(x);
	y = FIXED2INT(y);

	if (drawing_flags & DFLAG_CAM_FOLLOW) {
		x -= cam_x - 120;
		y -= cam_y - 80;
	}

	int shape = shapes[sprite];

	if (shape == UNLOADED_SPRITE)
		return;

	if (x + shape_width[shape] <= 0 || x > 240 ||
		y + shape_height[shape] <= 0 || y > 160)
		return;

	obj_set_attr(sprite_pointer,
				 ((shape & 0xC) << 12) | SPRITE_Y(y),
				 ((shape & 0x3) << 14) | SPRITE_X(x) | flip,
				 ATTR2_PALBANK(pal) | ATTR2_PRIO(prio) | (indexes[sprite]));

	++sprite_pointer;
	++sprite_count;
}

#pragma endregion

#pragma region Backgrounds

#define TILES_CHANGED	0x1
#define MAPPING_CHANGED 0x2

#define SCREENBLOCK_UPDATED 0x1

int layer_updates;
int foreground_count;

#define LAYER_SIZE(n) ((layers[n].gba_meta & 0xC000) >> 14)

#define TILESET_SIZE(n)		 (n->tile_meta & 0xFF)
#define TILESET_OFFSET(n)	 ((n->tile_meta & 0xFF00) >> 8)
#define TILESET_SET(n, o, s) n->tile_meta = (((o)&0xFF) << 8) | ((s)&0xFF)

// Layer functions that don't need to be finalized
void set_layer_visible(int layer, bool visible) {
	layer = 1 << (layer + 8);

	REG_DISPCNT = (REG_DISPCNT & ~layer) | (layer * visible);
}
void set_layer_priority(int layer, int prio) {
	// If the layer already has that priority, don't change anything
	if ((layers[layer].gba_meta & BG_PRIO_MASK) == prio)
		return;

	int i;

	// Find the layer that has the same priority
	for (i = 0; i < 4; ++i) {
		if (i == layer)
			continue;

		if ((layers[i].gba_meta & BG_PRIO_MASK) == prio) {
			layers[i].gba_meta = (layers[i].gba_meta & ~BG_PRIO_MASK) | (layers[layer].gba_meta & BG_PRIO_MASK);
			break;
		}
	}

	layers[layer].gba_meta = (layers[layer].gba_meta & ~BG_PRIO_MASK) | prio;
}
void set_layer_size(int layer, int size) {

	layers[layer].gba_meta = (layers[layer].gba_meta & ~BG_SIZE_MASK) | BG_SIZE(size);
	layer_updates |= SCREENBLOCK_UPDATED;
}
void change_layer_type(int layer, int type) {

	if ((LAYER_GET_TYPE(layers[layer]) != LStyle_FG) == (type == LStyle_FG)) {
		foreground_count += (type == LStyle_FG) ? 1 : -1;
	}

	layers[layer].meta = (layers[layer].meta & ~LAYER_TYPE_MASK) | type;
	layer_updates |= SCREENBLOCK_UPDATED;
}

// Layer functions that require finalization
void load_background(BackgroundLayer* layer, unsigned int* tiles, unsigned int tile_len, unsigned short* mapping, int size) {
	load_background_tiles(layer, tiles, tile_len, size);

	layer->map_ptr = mapping;
	layer->tile_meta |= MAPPING_CHANGED;
}
void load_background_tiles(BackgroundLayer* layer, unsigned int* tiles, unsigned int tile_len, int size) {
	if (layer->tile_ptr != tiles) {
		layer->tile_ptr = tiles;
		// if (index == 0)
		// 	TILESET_SET(layer, 0, tile_len);
		// else
		// 	TILESET_SET(layer, TILESET_SIZE(index - 1) + TILESET_OFFSET(index - 1), tile_len);

		// layer->tile_ptr = tiles;
		// layer->tile_meta &= ~0x30000; // size;
		// layer->tile_meta |= size << 16;

		// for (int i = index + 1; i < 4; ++i) {
		// 	TILESET_SET(i, TILESET_SIZE(i - 1) + TILESET_OFFSET(i - 1), TILESET_SIZE(i));
		// 	layers[i].tile_meta |= MAPPING_CHANGED;
		// }
	}
}

void finalize_layers() {

	int layerCount = 4;

	if (layer_updates) {

		if (layer_updates & SCREENBLOCK_UPDATED) {
			int sbb = 32;
			int size, cb;
			int i;

			for (i = 0; i < layerCount; ++i) {
#define lmask 0x1F0C

				int meta = layers[i].gba_meta;

				switch (LAYER_GET_TYPE(layers[i])) {
					case LStyle_FG:
						sbb--;

						cb = FG_TILESET;

						meta = (meta & ~lmask) | BG_SBB(sbb) | BG_CBB(cb);

						break;
					case LStyle_BG:
						size = LAYER_SIZE(i);
						if (size < 2) // sizes 2 and 3 use less SBs
							size++;
						sbb -= size;

						cb = BG_TILESET;

						BackgroundLayer* bg = (BackgroundLayer*)&layers[i];

						if (bg->tile_meta & TILES_CHANGED) {
							memcpy(&tile_mem[BG_TILESET][TILESET_OFFSET(bg)], bg->tile_ptr, TILESET_SIZE(bg) << 5);
						}
						if (bg->tile_meta & MAPPING_CHANGED) {
							int index = 32 * 32 * size;

							int offset			  = TILESET_OFFSET(bg);
							unsigned short *block = se_mem[sbb], *mapping = bg->map_ptr;

							while (index) {

								--index;

								block[index] = mapping[index] + offset;
							}
						}

						meta = (meta & ~lmask) | BG_SBB(sbb) | BG_CBB(cb);
						break;
					case LStyle_Free:
						break;
				}
#undef lmask

				layers[i].gba_meta = meta;
				REG_BGCNT[i]	   = meta;
			}

			sbb -= 8;
			bg_tile_allowance = sbb << 5;
		}

		layer_updates = 0;
	}
}

#pragma endregion

void init_drawing() {
	is_rendering = 0;

	foreground_count = 0;

	layers[0].meta = LAYER_META_INIT(0, LStyle_Free, true);
	layers[1].meta = LAYER_META_INIT(1, LStyle_Free, true);
	layers[2].meta = LAYER_META_INIT(2, LStyle_Free, true);
	layers[3].meta = LAYER_META_INIT(3, LStyle_Free, true);

	layers[0].gba_meta = BG_PRIO(0);
	layers[1].gba_meta = BG_PRIO(1);
	layers[2].gba_meta = BG_PRIO(2);
	layers[3].gba_meta = BG_PRIO(3);

	oam_init(obj_buffer, SPRITE_LIMIT);
	sprite_pointer = (OBJ_ATTR*)&obj_buffer;
	int i;

	indexes[0] = BANK_MEM_START;
	shapes[0]  = UNLOADED_SPRITE;
	for (i = 1; i < BANK_LIMIT; ++i) {
		indexes[i] = 0x8000;
		ordered[i] = i;
		shapes[i]  = UNLOADED_SPRITE;
	}

	layer_updates = SCREENBLOCK_UPDATED;
}

void begin_drawing() {
	int layerCount = 4; // TODO: Allow for affine layers, meaning this will be less than 4

	is_rendering = 1;

	finalize_layers();

	int i;

	for (i = 0; i < BANK_LIMIT; ++i) {
		if (wait_to_load[i]) {
			load_sprite_at(wait_to_load[i] & 0x0FFFFFFF, i, (wait_to_load[i] >> 28));

			wait_to_load[i] = 0;
		}

		// Don't animate sprites if update paused
#ifdef __DEBUG__
		if (ENGINE_DEBUGFLAG(PAUSE_UPDATES))
			break;
#endif

		if (!anim_bank[i])
			continue;

		anim_meta[i]--;

		if (!(anim_meta[i] & 0xF)) {
			// Reset counter
			anim_meta[i] |= (anim_meta[i] & 0xF0) >> 4;

			// Update the frame index
			anim_meta[i] = ((anim_meta[i] + 0x100) & 0xF00) + (anim_meta[i] & ~0xF00);

			// Reset the frame index
			if ((anim_meta[i] & 0xF00) > ((anim_meta[i] & 0xF000) >> 4)) {
				anim_meta[i] &= ~0xF00;
			}

			unsigned int* ptr = anim_bank[i];
			int offset		  = (anim_meta[i] & 0xF00) >> 8;
			int shape		  = (anim_meta[i] & 0xF0000) >> 16;

			load_sprite_at(&anim_bank[i][(offset * (shape2size[shape] >> 2))], i, shape);

			anim_bank[i] = ptr;
		}
	}
}
void end_drawing() {
	is_rendering = 0;

	if (sprite_count < prev_sprite_count) {
		int i = sprite_count;
		for (; i < prev_sprite_count; ++i) {
			sprite_pointer->attr0 = 0x0200;
			++sprite_pointer;
		}
	}
	prev_sprite_count = sprite_count;
	sprite_count	  = 0;
	affine_count	  = 0;

	oam_copy(oam_mem, obj_buffer, SPRITE_LIMIT);

	sprite_pointer = (OBJ_ATTR*)&obj_buffer;
}
