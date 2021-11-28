#include "input.h"
#include "tonc_vscode.h"

int last_pressed[10];

#define MAX_BUFFER		0xFF

#define LAST_HIT		0x101

void update_presses() {
	int i;
	
	for (i = 0; i < 10; ++i) {
		if ((last_pressed[i] >= 0 && last_pressed[i] <= MAX_BUFFER) || last_pressed[i] == LAST_HIT)
			last_pressed[i]++;
		if (key_hit((1 << i))) {
			last_pressed[i] = 0;
		}
		else if (!KEY_DOWN_NOW((1 << i))) {
			last_pressed[i] = -1;
		}
	}
	
}

int key_pressed(int key, int buffer) {
	int i;
	
	buffer &= MAX_BUFFER;
	
	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)) || last_pressed[i] < 0)
			continue;
		if (last_pressed[i] <= buffer || last_pressed[i] == LAST_HIT)
			return 1;
	}
	
	return 0;
}
void clear_buffer(int key) {
	int i;
	
	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)))
			continue;
		last_pressed[i] = LAST_HIT;
	}
}
void clear_pressed(int key) {
	int i;
	
	for (i = 0; i < 10; ++i) {
		if (!(key & (1 << i)))
			continue;
		last_pressed[i] = -1;
	}
}