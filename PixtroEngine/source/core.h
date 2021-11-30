#pragma once
#include "tonc_vscode.h"

#include "engine.h"

// ---- Entities ----
// The basic entity structure.
typedef struct
{
	int x, y, vel_x, vel_y;
	// Width of the entity in pixels
	unsigned short width;
	// Height of the entity in pixels
	unsigned short height;
	// The unique id each entity gets.  Determines what kind of entity it is and which level it was first loaded from.
	unsigned int ID;
	unsigned int collision_flags;

	unsigned int flags[5];
} ALIGN4 Entity;

#define ENT_TYPE(n) (entities[n].ID & 0xFF)

#define ENT_FLAG(name, n) (entities[n].flags[4] & ENT_##name##_FLAG)
#define ENABLE_ENT_FLAG(name, n) entities[n].flags[4] |= ENT_##name##_FLAG
#define DISABLE_ENT_FLAG(name, n) entities[n].flags[4] &= ~ENT_##name##_FLAG

// If enabled, this entity won't be unloaded when moving between levels
#define PERSISTENT
// If enabled, this entity will update as normal
#define ACTIVE

#define ENT_LOADED_FLAG 0x00000001
#define ENT_PERSISTENT_FLAG 0x00000002
#define ENT_ACTIVE_FLAG 0x00000004
#define ENT_VISIBLE_FLAG 0x00000008
#define ENT_DETECT_FLAG 0x00000010
#define ENT_COLLIDE_FLAG 0x00000011

#define LOAD_ENTITY(name, i)           \
	entity_inits[i] = &name##_init;    \
	entity_update[i] = &name##_update; \
	entity_render[i] = &name##_render

extern unsigned int max_entities;

extern int (*entity_inits[32])(unsigned int actor_index, unsigned char *data, unsigned char *is_loading);
extern Entity entities[ENTITY_LIMIT];
extern void (*entity_update[32])(unsigned int index);
extern void (*entity_render[32])(unsigned int index);

// ---- LAYERS ----
// Layer struct
typedef struct Layer
{
	int pos[8];	 // The offsets of the lerp, excluding camera
	int lerp[8]; // combines both x and y, ranging from 0 - 0x100.

	unsigned int meta;
	unsigned int tile_meta; // the size and offset of the tiles used.  8 bits for offset, 8 bits for size
	unsigned int *tile_ptr;
	unsigned short *map_ptr;

} Layer;

extern int layer_count, layer_line[7], layer_index;
extern int bg_tile_allowance;

extern Layer layers[4];
extern int foreground_count;

// Macro to help load backgrounds easier.
#define LOAD_BG(bg, n) load_background(n, BGT_##bg, BGT_##bg##_len, BG_##bg, BG_##bg##_size)

#define FG_TILESET 0
#define BG_TILESET 1

void set_layer_visible(int layer, bool vis);
void set_layer_priority(int layer, int prio);
void set_foreground_count(int count);
void load_background(int index, unsigned int *tiles, unsigned int tile_len, unsigned short *mapping, int size);
void finalize_layers();

// ---- ENGINE ----
//
extern unsigned int game_life, levelpack_life, level_life;
extern unsigned int game_freeze;
extern unsigned int engine_flags;

#ifdef __DEBUG__

#define GAME_DFLAG_WAITING 0x00000001

#define ENG_DFLAG_PAUSE_UPDATES 0x00000001

extern unsigned int debug_engine_flags, debug_game_flags;
#define ENGINE_DEBUGFLAG(name) (debug_engine_flags & ENG_DFLAG_##name)
#define SET_DEBUGFLAG(name) (debug_game_flags |= GAME_DFLAG_##name)
#define REMOVE_DEBUGFLAG(name) (debug_game_flags &= ~GAME_DFLAG_##name)

#endif

// Enabled when the engine is loading levels async
#define LOADING_ASYNC
#define ENG_FLAG_LOADING_ASYNC 0x00000001

#define ENGINE_HAS_FLAG(name) (engine_flags & ENG_FLAG_##name)
#define SET_ENGINE_FLAG(name) (engine_flags |= ENG_FLAG_##name)
#define REMOVE_ENGINE_FLAG(name) (engine_flags &= ~ENG_FLAG_##name)

// The size of brick in a level.
#ifdef LARGE_TILES

#define BLOCK_SIZE 16
#define BLOCK_SHIFT 4

#else

#define BLOCK_SIZE 8
#define BLOCK_SHIFT 3

#endif

extern void (*custom_update)(void);
extern void (*custom_render)(void);

void pixtro_init();
void pixtro_update();
void pixtro_render();

// ---- Levels ----

void load_level_pack(unsigned int *level_pack, int section);
void load_level_pack_async(unsigned int *level_pack, int section);
void move_to_level(int level, int section);

// Others
void open_file(int file);
void save_file();
void reset_file();

void save_settings();
void reset_settings();

char char_from_file(int index);
short short_from_file(int index);
int int_from_file(int index);
void char_to_file(int index, char value);
void short_to_file(int index, short value);
void int_to_file(int index, int value);

char char_from_settings(int index);
short short_from_settings(int index);
int int_from_settings(int index);
void char_to_settings(int index, char value);
void short_to_settings(int index, short value);
void int_to_settings(int index, int value);

INLINE void key_mod(u32 key);
INLINE void key_mod2(u32 key);

INLINE void key_mod(u32 key)
{
	__key_curr = key & KEY_MASK;
}

INLINE void key_mod2(u32 key)
{
	__key_prev = key & KEY_MASK;
}
