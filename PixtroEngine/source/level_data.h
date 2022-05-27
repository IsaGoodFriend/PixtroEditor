#pragma once

extern char level_meta[128];

// ---- Levels ----
void load_level_pack(unsigned int* level_pack);
void load_level(int level);

void move_cam();
void reset_cam();
int add_entity(int x, int y, int type);