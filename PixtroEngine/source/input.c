#include "input.h"

char last_pressed[10];
int game_control;

#define GAME_INPUT_MASK			 0x3FF
#define GAME_INPUT_PREV_MASK	 0xFFC00
#define GAME_INPUT_ENABLED_MASK	 0x100000
#define GAME_INPUT_ENABLED_SHIFT 20

#define MAX_BUFFER 0xFF

#define LAST_VALID_PRESS 0xFE
#define UNPRESSED		 0xFF

void update_presses() {
	int enabled	 = game_control & GAME_INPUT_ENABLED_MASK;
	game_control = ((game_control & GAME_INPUT_MASK) << GAME_INPUT_ENABLED_SHIFT) | enabled;

	int i;

	for (i = 0; i < 10; ++i) {
		if (last_pressed[i] != UNPRESSED) {

			last_pressed[i]++;
			if (last_pressed[i] == LAST_VALID_PRESS)
				last_pressed[i]++;
		}
		bool hit = 0;

		if (game_control & GAME_INPUT_ENABLED_MASK) {
			hit = (game_control & (1 << i)) && !(game_control & (0x400 << i));
		} else {
			hit = key_hit((1 << i));
		}

		if (hit) {
			last_pressed[i] = 0;
		} else if (!KEY_DOWN_NOW((1 << i))) {
			last_pressed[i] = UNPRESSED;
		}
	}
}
void update_inputs() {
	update_presses();
}
void init_inputs() {
	game_control = 0;

	for (int i = 0; i < 10; ++i) {
		last_pressed[i] = UNPRESSED;
	}
}

void set_game_control(bool enabled) {
	game_control = enabled << GAME_INPUT_ENABLED_SHIFT;
}
void enable_keys(int keys) {
	game_control |= keys;
}
void disable_keys(int keys) {
	game_control &= ~keys;
}

int pixtro_key_check(int key) {
	if (game_control & GAME_INPUT_ENABLED_MASK) {
		return game_control & 0x3FF & key;
	} else {
		return key_is_down(key);
	}
}
int pixtro_tri_horz(void) {
	if (game_control & GAME_INPUT_ENABLED_MASK) {
		return bit_tribool(game_control, KI_RIGHT, KI_LEFT);
	} else {
		return key_tri_horz();
	}
}
int pixtro_tri_vert(void) {
	if (game_control & GAME_INPUT_ENABLED_MASK) {
		return bit_tribool(game_control, KI_DOWN, KI_UP);
	} else {
		return key_tri_vert();
	}
}
int pixtro_tri_shoulder(void) {
	if (game_control & GAME_INPUT_ENABLED_MASK) {
		return bit_tribool(game_control, KI_R, KI_L);
	} else {
		return key_tri_shoulder();
	}
}
int pixtro_tri_fire(void) {
	if (game_control & GAME_INPUT_ENABLED_MASK) {
		return bit_tribool(game_control, KI_A, KI_B);
	} else {
		return key_tri_shoulder();
	}
}
int key_pressed(int key, int buffer) {
	int i;

	if (buffer > LAST_VALID_PRESS)
		return true;

	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)) || last_pressed[i] == UNPRESSED)
			continue;
		if (last_pressed[i] <= buffer || last_pressed[i] == LAST_VALID_PRESS)
			return true;
	}

	return false;
}
void clear_buffer(int key) {
	int i;

	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)))
			continue;
		last_pressed[i] = LAST_VALID_PRESS;
	}
}
void clear_press(int key) {
	int i;

	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)))
			continue;
		last_pressed[i] = UNPRESSED;
	}
}