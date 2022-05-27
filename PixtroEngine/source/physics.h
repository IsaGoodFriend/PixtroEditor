#pragma once

#include "core.h"
#include "tonc_vscode.h"

extern unsigned short* tile_types;

extern int (*physics_code[255])(void);
extern bool (*collide_code[255])(int, int, int, int);

extern unsigned int entity_physics(Entity* ent, int hit_mask);
extern unsigned int collide_rect(int x, int y, int width, int height, int hit_mask);
extern unsigned int collide_entity(unsigned int ID);
