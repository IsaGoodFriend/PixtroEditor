#pragma once

extern char level_meta[128];

// ---- Levels ----
void load_level_pack(unsigned int* level_pack);
void load_level(int level);

extern bool (*physics_code[255])(int x, int y, int width, int height, int vel, bool move_vert);
extern bool (*collide_code[255])(int x, int y, int width, int height);

void move_cam();
void reset_cam();
int add_entity(int x, int y, int type);