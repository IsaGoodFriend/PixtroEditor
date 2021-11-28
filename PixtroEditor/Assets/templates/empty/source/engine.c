#include "engine.h"
#include "tonc_vscode.h"

#include "pixtro_basic.h"

#include "levels.h"
#include "load_data.h"

void on_update() {
}

// Run before anything else happens in the game
void init() {
	
	set_foreground_count(1);
	finalize_layers();
	
	custom_update = &on_update;
	
}

// Run the first time the game is initialized.  Mainly used for setting default settings
void init_settings() {
	
}