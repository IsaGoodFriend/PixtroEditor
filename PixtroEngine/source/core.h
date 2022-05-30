#pragma once
#include "tonc_vscode.h"

#include "coroutine.h"
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

#define ENT_FLAG(name, n)		  (entities[n].ID & ENT_##name##_FLAG)
#define ENABLE_ENT_FLAG(name, n)  entities[n].ID |= ENT_##name##_FLAG
#define DISABLE_ENT_FLAG(name, n) entities[n].ID &= ~(ENT_##name##_FLAG)

// If enabled, this entity won't be unloaded when moving between levels
#define PERSISTENT
// If enabled, this entity will update as normal
#define ACTIVE

#define ENT_LOADED_FLAG		0x00010000
#define ENT_PERSISTENT_FLAG 0x00020000
#define ENT_ACTIVE_FLAG		0x00040000
#define ENT_VISIBLE_FLAG	0x00080000
#define ENT_DETECT_FLAG		0x00100000
#define ENT_COLLIDE_FLAG	0x00110000

#define LOAD_ENTITY(name, i)           \
	entity_inits[i]	 = &name##_init;   \
	entity_update[i] = &name##_update; \
	entity_render[i] = &name##_render

extern unsigned int max_entities;

extern int (*entity_inits[32])(unsigned int actor_index, unsigned char* data, unsigned char* is_loading);
extern Entity entities[ENTITY_LIMIT];
extern void (*entity_update[32])(unsigned int index);
extern void (*entity_render[32])(unsigned int index);

// ---- ENGINE ----
//
extern unsigned int game_life, levelpack_life, level_life;
extern unsigned int game_freeze;
extern unsigned int engine_flags;

extern void (*onfinish_async_loading)();
extern void (*onfade_function)(Routine*);
void start_fading();

#ifdef __DEBUG__

#define GAME_DFLAG_WAITING 0x00000001

#define ENG_DFLAG_PAUSE_UPDATES 0x00000001

extern unsigned int debug_engine_flags, debug_game_flags;

#define ENGINE_DEBUGFLAG(name) (debug_engine_flags & ENG_DFLAG_##name)
#define SET_DEBUGFLAG(name)	   (debug_game_flags |= GAME_DFLAG_##name)
#define REMOVE_DEBUGFLAG(name) (debug_game_flags &= ~GAME_DFLAG_##name)

extern unsigned int debug_flags;

#endif

// Enabled when the engine is loading levels async
#define LOADING_ASYNC
#define ENG_FLAG_LOADING_ASYNC 0x00000001

#define ENGINE_HAS_FLAG(name)	 (engine_flags & ENG_FLAG_##name)
#define SET_ENGINE_FLAG(name)	 (engine_flags |= ENG_FLAG_##name)
#define REMOVE_ENGINE_FLAG(name) (engine_flags &= ~ENG_FLAG_##name)

// The size of brick in a level.
#ifdef LARGE_TILES

#define BLOCK_SIZE	16
#define BLOCK_SHIFT 4

#else

#define BLOCK_SIZE	8
#define BLOCK_SHIFT 3

#endif

extern void (*custom_update)(void);
extern void (*custom_render)(void);

void pixtro_init();
void pixtro_update();
void pixtro_render();

// Others
void open_file(int file);
void save_file();
void reset_file();

void save_settings();
void reset_settings();

char char_from_file(int index);
short short_from_file(int index);
int int_from_file(int index);
long long_from_file(int index);
void char_to_file(int index, char value);
void short_to_file(int index, short value);
void int_to_file(int index, int value);
void long_to_file(int index, long value);

char char_from_settings(int index);
short short_from_settings(int index);
int int_from_settings(int index);
void char_to_settings(int index, char value);
void short_to_settings(int index, short value);
void int_to_settings(int index, int value);

INLINE void key_mod(u32 key);
INLINE void key_mod2(u32 key);

INLINE void key_mod(u32 key) {
	__key_curr = key & KEY_MASK;
}

INLINE void key_mod2(u32 key) {
	__key_prev = key & KEY_MASK;
}
