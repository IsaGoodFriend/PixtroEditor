#pragma once

#include "math.h"
#include "engine.h"

extern int drawing_flags;

// Drawing flags
#define DFLAG_CAM_FOLLOW 0x0001 // Do sprites use cam position data?
#define DFLAG_CAM_BOUNDS 0x0002 // Keep the camera in the bounds of the level? (ONLY DISABLE IF YOU KNOW WHAT YOU'RE DOING)

#define SET_DRAWING_FLAG(name) drawing_flags |= DFLAG_##name;
#define CLEAR_DRAWING_FLAG(name) drawing_flags &= ~DFLAG_##name;
#define DRAWING_FLAG_ENABLED(name) (drawing_flags & DFLAG_##name);

// Sprite shapes
#define SPRITE8x8 0
#define SPRITE16x16 1
#define SPRITE32x32 2
#define SPRITE64x64 3

#define SPRITE16x8 4
#define SPRITE32x8 5
#define SPRITE32x16 6
#define SPRITE64x32 7

#define SPRITE8x16 8
#define SPRITE8x32 9
#define SPRITE16x32 10
#define SPRITE32x64 11

#define FLIP_NONE 0
#define FLIP_X 0x1000
#define FLIP_Y 0x2000
#define FLIP_XY 0x3000

extern int cam_x, cam_y;

int load_sprite(unsigned int *sprite, int shape);
int load_anim_sprite(unsigned int *sprites, int shape, int frames, int speed);
void load_sprite_at(unsigned int *sprite, int index, int shape);
void load_anim_sprite_at(unsigned int *sprites, int index, int shape, int frames, int speed);
void load_obj_pal(unsigned short *pal, int palIndex);
void load_bg_pal(unsigned short *pal, int palIndex);

#define LOAD_TILESET(name) load_tileset((unsigned int *)TILESET_##name, (unsigned short *)TILE_MAPPING_##name, (unsigned int *)TILECOLL_##name, TILESET_##name##_len)
void load_tileset(unsigned int *tiles, unsigned short *mapping, unsigned int *collision, int count);

void draw(int x, int y, int sprite, int flip, int prio, int pal);
void draw_affine(AffineMatrix matrix, int sprite, int prio, int pal);
void draw_affine_big(AffineMatrix matrix, int sprite, int prio, int pal);

void init_drawing();
void end_drawing();

void update_camera();
