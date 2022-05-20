#pragma once
#include "tonc_vscode.h"

void set_game_control(bool enabled);
void enable_keys(int keys);
void disable_keys(int keys);

int pixtro_tri_horz(void);
int pixtro_tri_vert(void);
int pixtro_tri_shoulder(void);
int pixtro_tri_fire(void);

int pixtro_key_check(int key);
int key_pressed(int key, int buffer);
void clear_buffer(int key);
void clear_press(int key);
