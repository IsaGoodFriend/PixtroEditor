#pragma once

#include "core.h"
#include "entities.h"
#include "tonc_vscode.h"

extern unsigned short* tile_types;

extern unsigned int entity_physics(Entity* ent, int hit_mask);
extern unsigned int collide_rect(int x, int y, int width, int height, int hit_mask);
extern int collide_entity(unsigned int index);
