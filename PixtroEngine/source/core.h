#pragma once
#include "tonc_vscode.h"

#include "coroutine.h"
#include "engine.h"

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

#endif

extern unsigned int debug_flags;

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
