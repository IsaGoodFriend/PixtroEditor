#include "engine.h"
#include "tonc_vscode.h"

#include "pixtro_basic.h"

#include "levels.h"
#include "load_data.h"
#include "character.h"
#include "coroutine.h"

// Run before anything else happens in the game
void init()
{
	set_foreground_count(1);
	finalize_layers();

	//LOAD_BG(sample_ase, 1);
	LOAD_ENTITY(character, 0);

	load_bg_pal(PAL_test, 0);
	load_obj_pal(PAL_character, 0);
	load_sprite(SPR_char_idle, 0, SPRITE32x32);

	load_level_pack(PACK_test, 0);

	LOAD_TILESET(all);

	move_to_level(0, 0);
	reset_cam();
}

// Run the first time the game is initialized.  Mainly used for setting default settings
void init_settings()
{
}