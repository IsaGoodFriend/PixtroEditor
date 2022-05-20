#include <string.h>

#include "core.h"
#include "coroutine.h"
#include "graphics.h"
#include "load_data.h"
#include "loading.h"
#include "math.h"
#include "physics.h"

int layer_count, layer_line[7], layer_index;
int bg_tile_allowance;

Layer layers[4];

// Entities
unsigned int max_entities;

int (*entity_inits[32])(unsigned int actor_index, unsigned char* data, unsigned char* is_loading);
Entity entities[ENTITY_LIMIT];
void (*entity_update[32])(unsigned int index);
void (*entity_render[32])(unsigned int index);

// Level data
char level_meta[128];
extern unsigned int lvl_width, lvl_height;
extern unsigned short* tileset_data;
unsigned short *loaded_levels_a[64], *loaded_levels_b[64];
unsigned int* loading_levelpack;
extern unsigned char* level_rom;
extern int level_loading;
extern unsigned short* level_ram;
extern unsigned int unloaded_entities[64];

Routine loading_routine;
void (*onfade_function)(Routine*);
void (*onfinish_async_loading)();
Routine onfade_routine;

// Engine stuff
unsigned int game_life, levelpack_life, level_life;
unsigned int game_freeze;
unsigned int engine_flags;
#ifdef __DEBUG__
unsigned int debug_engine_flags, debug_game_flags;
int current_level_index;
#endif
int fade_timer;

// Saveram
#define SAVE_INDEX (save_file_number * SAVEFILE_LEN) + SETTING_LEN

char save_data[SAVEFILE_LEN - 1], settings_file[SETTING_LEN - 1];
int save_file_number;

// Other
void (*custom_update)(void);
void (*custom_render)(void);

void load_settings();
void interrupt();
void load_background_tiles(int index, unsigned int* tiles, unsigned int tile_len, int size);

extern void set_entities_location();

extern void init();
extern void init_settings();

extern void rng_seed(unsigned int seed1, unsigned int seed2, unsigned int seed3);

extern void init_inputs();
extern void begin_drawing();
extern void update_particles();
extern void update_inputs();
extern void load_entities();

// Initialize the game
void pixtro_init() {
	loading_routine.at = -1;
	loaded_levels_a[0] = (unsigned short*)0x02020000;
	loaded_levels_b[0] = (unsigned short*)0x02030000;

	init_inputs();

	set_loading_region(0);

	// Set the RNG seeds.  Values can be any positive integer
	rng_seed(RNG_SEED_1, RNG_SEED_2, RNG_SEED_3);

	// Initialize graphics settings.  Must run before anything visual happens
	init_drawing();

	// Load in settings, and initialize settings if running game for the first time
	load_settings();

	// Display everything
	REG_DISPCNT = DCNT_BG0 | DCNT_BG1 | DCNT_BG2 | DCNT_BG3 | DCNT_OBJ | DCNT_OBJ_1D;

	// Initialize the engine with user's code
	init();
}

// The game's update loop
void pixtro_update() {
	// Skip running update if editor wants game paused
#ifdef __DEBUG__
	if (ENGINE_DEBUGFLAG(PAUSE_UPDATES))
		return;
#endif

	int i;

	// Increment game's life counter
	game_life++;

	// Update inputs
	update_inputs();

	if (fade_timer == 10)
		fade_timer = 0;

	if (fade_timer == 5) {
		if (onfade_function) {
			onfade_function(&onfade_routine);

			if (onfade_routine.at == -1) {
				onfade_function = NULL;
				fade_timer		= 6;
			}
		} else {
			fade_timer = 6;
		}
	} else {
		// Update engine when not fading
		if (!fade_timer) {
			if (game_freeze <= 0) {
				// Run over every active entity and run it's custom update
				for (i = 0; i < max_entities; ++i) {
					if (!ENT_FLAG(ACTIVE, i) || !ENT_FLAG(LOADED, i) || !entity_update[ENT_TYPE(i)])
						continue;

					entity_update[ENT_TYPE(i)](i);
				}
			} else {
				game_freeze--;
			}

			// Custom update if desired
			if (custom_update)
				custom_update();
		} else
			fade_timer++;
	}

	if (ENGINE_HAS_FLAG(LOADING_ASYNC)) {
		async_loading();
	}
}

// Rendering the game
void pixtro_render() {
	// Setting the background offset index to 0
	layer_index = 0;

	int i;

	// Set the camera position and load in level if the camera has moved (and if there is any level)
	move_cam();
	begin_drawing();

	// Update and render particles
	update_particles();

	// Render each visible entity
	for (i = 0; i < max_entities; ++i) {
		if (!ENT_FLAG(VISIBLE, i) || !ENT_FLAG(LOADED, i) || !entity_render[ENT_TYPE(i)])
			continue;

		SET_DRAWING_FLAG(CAM_FOLLOW);

		entity_render[ENT_TYPE(i)](i);
	}

	// Custom render if desired
	if (custom_render)
		custom_render();

	// Finalize the graphics and prepare for the next cycle
	end_drawing();

	if (fade_timer) {
		fade_black(fade_timer <= 5 ? fade_timer : (10 - fade_timer));
	}
}

// Level functions
void move_to_level(int level, int section) {
#ifdef __DEBUG__
	current_level_index = level | (section << 8);
#endif

	tileset_data = section ? loaded_levels_b[level] : loaded_levels_a[level];
	lvl_width	 = tileset_data[0];
	lvl_height	 = tileset_data[1];

	load_entities();
}

#define load_initialize()           \
	if (loading_routine.at >= 0)    \
		return;                     \
	set_loading_region(section);    \
	reset_routine(loading_routine); \
	level_loading	  = 0;          \
	loading_levelpack = level_pack;

void load_level_pack(unsigned int* level_pack, int section) {
	load_initialize();

	while (loading_routine.at >= 0) {
		async_loading();
	}
}
void load_level_pack_async(unsigned int* level_pack, int section) {
	load_initialize();

	SET_ENGINE_FLAG(LOADING_ASYNC);
}
void async_loading() {

	int data = loading_levelpack[0];

	rt_begin(loading_routine);

	load_header((unsigned char*)data);
	loading_levelpack++;

	rt_while(data != 0);

	switch (data & 0xF) {
		case 1: // Set up for next level
			level_loading++;
			loaded_levels_a[level_loading] = level_ram;

			load_header((unsigned char*)loading_levelpack[1]);
			loading_levelpack++;
			break;
		case 2:
			load_midground();
			break;
		case 3:
			set_entities_location();

			break;
		case 4: // Load in tileset collision data
			{
				int i;
				// Is this needed?  Will there ever be a case where the game will read outside of the used tilesets?
				// for (i = 0; i < 256; ++i)
				//{
				//	tile_types[i] = 0;
				//}

				loading_levelpack++;
				for (i = 0; i < loading_levelpack[i] < 0x0FFFFFFF; ++i) {
					tile_types[i] = loading_levelpack[i];
					loading_levelpack++;
				}

				break;
			}
	}

	loading_levelpack++;

	rt_step();

	REMOVE_ENGINE_FLAG(LOADING_ASYNC);

	if (level_loading < 63) {
		level_loading++;
		loaded_levels_a[level_loading] = NULL;
	}

	if (onfinish_async_loading) {
		onfinish_async_loading();
		onfinish_async_loading = NULL;
	}

	rt_end();
}

// Basic engine functions
void routine_on_fade(void (*function)(Routine*)) {
	onfade_function = function;
	reset_routine(onfade_routine);
}
void start_fading() {
	if (fade_timer)
		return;
	fade_timer = 1;
}

// Settings File
void load_settings() {
	int index;

	if (sram_mem[0] == 0xFF) {

		sram_mem[0] = 0;

		for (index = 0; index < SETTING_LEN - 1; ++index) {
			settings_file[index] = 0;
		}

		init_settings();
		save_settings();
	} else {
		for (index = 0; index < SETTING_LEN - 1; ++index) {
			settings_file[index] = sram_mem[index + 1];
		}
	}
}
void save_settings() {
	int i;

	for (i = 0; i < SETTING_LEN - 1; ++i) {
		sram_mem[i + 1] = settings_file[i];
	}
}
void reset_settings() {
	int index;

	for (index = 0; index < SETTING_LEN; ++index) {
		sram_mem[index] = 0;
	}
}

// Save Files
void reset_file() {
	int index;
	int index2 = SAVE_INDEX;

	for (index = 0; index < SAVEFILE_LEN; ++index) {
		sram_mem[index + index2] = 0xFF;
	}
}
void save_file() {

	int index;
	int index2 = SAVE_INDEX;

	for (index = 0; index < SAVEFILE_LEN - 1; ++index) {
		sram_mem[index + index2 + 1] = save_data[index];
	}
}
void load_file() {

	int index  = 0;
	int index2 = SAVE_INDEX;

	if (sram_mem[index2] == 0xFF) {
		for (index = 0; index < SAVEFILE_LEN - 1; ++index) {
			save_data[index] = sram_mem[index + index2 + 1];
		}
	} else {
		sram_mem[index2] = 0;

		for (index = 0; index < SAVEFILE_LEN - 1; ++index) {
			save_data[index] = 0;
		}
		save_file();
	}
}
void open_file(int file) {
	save_file_number = file;
	load_file();
}

// Get and set from Save file
char char_from_file(int index) {
	return save_data[index];
}
short short_from_file(int index) {
	return (save_data[index]) + (save_data[index + 1] << 8);
}
int int_from_file(int index) {
	return (save_data[index]) + (save_data[index + 1] << 8) + (save_data[index + 2] << 16) + (save_data[index + 3] << 24);
}
void char_to_file(int index, char value) {
	save_data[index] = value;
}
void short_to_file(int index, short value) {
	int i;

	for (i = 0; i < 2; ++i) {
		save_data[index + i] = value & 0xFF;
		value >>= 8;
	}
}
void int_to_file(int index, int value) {
	int i;

	for (i = 0; i < 4; ++i) {
		save_data[index + i] = value & 0xFF;
		value >>= 8;
	}
}
// Get and set from settings
char char_from_settings(int index) {
	return settings_file[index];
}
short short_from_settings(int index) {
	return (settings_file[index]) + (settings_file[index + 1] << 8);
}
int int_from_settings(int index) {
	return (settings_file[index]) + (settings_file[index + 1] << 8) + (settings_file[index + 2] << 16) + (settings_file[index + 3] << 24);
}
void char_to_settings(int index, char value) {
	settings_file[index] = value;
}
void short_to_settings(int index, short value) {
	int i;

	for (i = 0; i < 2; ++i) {
		settings_file[index + i] = value & 0xFF;
		value >>= 8;
	}
}
void int_to_settings(int index, int value) {
	int i;

	for (i = 0; i < 4; ++i) {
		settings_file[index + i] = value & 0xFF;
		value >>= 8;
	}
}
