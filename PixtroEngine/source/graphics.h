#pragma once

#include "engine.h"
#include "math.h"

extern int drawing_flags;

// Drawing flags
#define DFLAG_CAM_FOLLOW 0x0001 // Do sprites use cam position data?
#define DFLAG_CAM_BOUNDS 0x0002 // Keep the camera in the bounds of the level? (ONLY DISABLE IF YOU KNOW WHAT YOU'RE DOING)

#define SET_DRAWING_FLAG(name)	   drawing_flags |= DFLAG_##name;
#define CLEAR_DRAWING_FLAG(name)   drawing_flags &= ~DFLAG_##name;
#define DRAWING_FLAG_ENABLED(name) (drawing_flags & DFLAG_##name);

#pragma region Backgrounds

typedef enum {
	LStyle_BG,
	LStyle_FG,
	LStyle_Free
} LayerStyle;

#define LAYER_TYPE_MASK	  0x0003
#define LAYER_TYPE_SHIFT  0
#define LAYER_TYPE(n)	  ((n) << LAYER_TYPE_SHIFT)
#define LAYER_GET_TYPE(n) ((n.meta & LAYER_TYPE_MASK) >> LAYER_TYPE_SHIFT)

#define LAYER_INDEX_MASK  0x000C
#define LAYER_INDEX_SHIFT 2
// No macro for changing index, that must stay constant (would make read only if I could)

#define LAYER_VISIBLE_MASK	0x8000
#define LAYER_VISIBLE_SHIFT 15
#define LAYER_VISIBLE(n)	((n) << LAYER_VISIBLE_SHIFT)
#define LAYER_IS_VISIBLE(n) ((n.meta & LAYER_VISIBLE_MASK) >> LAYER_VISIBLE_SHIFT)

#define LAYER_META_INIT(meta, t, vis) ((meta & ~LAYER_INDEX_MASK) | LAYER_TYPE(t) | LAYER_VISIBLE(vis))

typedef struct // Generic Layer data
{
	// 0-1 = Layer type.
	// 2-3 = Layer Index.  Read only
	// 4   = Char block 0 or 1
	// F   = Visible?
	unsigned short meta;
	unsigned short gba_meta;

	int x, y;

	int extra_data[5];

} Layer;
typedef struct // Foreground Layer
{
	// Generic layer data
	unsigned int meta;
	int x, y;

	unsigned int tile_meta; // the size and offset of the tiles used.  8 bits for offset, 8 bits for size
	unsigned int* tile_ptr;
	unsigned short* map_ptr;

	unsigned int extra_data[2];

} GameplayLayer;
typedef struct // Level Background
{
	unsigned int meta;
	int x, y;

	unsigned int tile_meta; // the size and offset of the tiles used.  8 bits for offset, 8 bits for size
	unsigned int* tile_ptr;
	unsigned short* map_ptr;

	unsigned int extra_data[2];

} BackgroundLayer;
typedef struct // Freestyle
{
	unsigned int meta;
	int x, y;

	unsigned int extra_data[5];

} FreestyleLayer;
#pragma endregion

extern int layer_count, layer_line[7], layer_index;
// The amount of tiles available after removing screenblocks
extern int bg_tile_allowance;

extern Layer layers[4];

extern int cam_x, cam_y;

#define LOAD_BG(bg, n) load_background(n, BGT_##bg, BGT_##bg##_len, BG_##bg, BG_##bg##_size)

#define FG_TILESET 0
#define BG_TILESET 1

void set_layer_visible(int layer, bool vis);
void set_layer_priority(int layer, int prio);
void set_layer_size(int layer, int size);
void change_layer_type(int layer, int type);

void load_background(BackgroundLayer* layer, unsigned int* tiles, unsigned int tile_len, unsigned short* mapping, int size);

// ---- Sprites ----

// Sprite shapes
#define SPRITE8x8	0
#define SPRITE16x16 1
#define SPRITE32x32 2
#define SPRITE64x64 3

#define SPRITE16x8	4
#define SPRITE32x8	5
#define SPRITE32x16 6
#define SPRITE64x32 7

#define SPRITE8x16	8
#define SPRITE8x32	9
#define SPRITE16x32 10
#define SPRITE32x64 11

#define FLIP_NONE 0
#define FLIP_X	  0x1000
#define FLIP_Y	  0x2000
#define FLIP_XY	  0x3000

int load_sprite(unsigned int* sprite, int shape);
int load_anim_sprite(unsigned int* sprites, int shape, int frames, int speed);
void load_sprite_at(unsigned int* sprite, int index, int shape);
void load_anim_sprite_at(unsigned int* sprites, int index, int shape, int frames, int speed);
void load_obj_pal(unsigned short* pal, int palIndex);
void load_bg_pal(unsigned short* pal, int palIndex);

// ---- Tilesets ----
#define LOAD_TILESET(name) load_tileset((unsigned int*)TILESET_##name, (unsigned short*)TILE_MAPPING_##name, (unsigned int*)TILECOLL_##name, TILESET_##name##_len, TILESET_##name##_uvlen)
void load_tileset(unsigned int* tiles, unsigned short* mapping, unsigned int* collision, int count, int uvcount);

void draw(int x, int y, int sprite, int flip, int prio, int pal);
void draw_affine(AffineMatrix matrix, int sprite, int prio, int pal);
void draw_affine_big(AffineMatrix matrix, int sprite, int prio, int pal);

void init_drawing();
void end_drawing();

void update_camera();
