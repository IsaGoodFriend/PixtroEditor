#pragma once

#include "core.h"

#define SET_TILE_DATA(i, shape, coll) tile_types[i] = (shape) | (coll << 8)

extern unsigned int tile_types[256];

extern unsigned int entity_physics(Entity *ent, int hit_mask);
extern unsigned int collide_rect(int x, int y, int width, int height, int hit_mask);
extern unsigned int collide_entity(unsigned int ID);
