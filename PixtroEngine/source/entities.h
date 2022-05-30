#pragma once

#include "engine.h"
#include "tonc_vscode.h"

// ---- Entities ----
// The basic entity structure.

typedef struct
{
	int x, y, vel_x, vel_y;
	// Width of the entity in pixels
	unsigned short width;
	// Height of the entity in pixels
	unsigned short height;
	// The unique id each entity gets.  Determines what kind of entity it is and which level it was first loaded from.
	unsigned int ID;

	unsigned int flags[6];
} ALIGN4 Entity;

#define ENT_TYPE(n) (entities[n].ID & 0x1F)

#define ENT_FLAG(name, n)		  (entities[n].ID & ENT_##name##_FLAG)
#define ENABLE_ENT_FLAG(name, n)  entities[n].ID |= ENT_##name##_FLAG
#define DISABLE_ENT_FLAG(name, n) entities[n].ID &= ~(ENT_##name##_FLAG)

// If enabled, this entity won't be unloaded when moving between levels
#define PERSISTENT
// If enabled, this entity will update as normal
#define ACTIVE
// If enabled, this entity will be detected when checking for entity collisions
#define DETECT

#define ENT_ID_TYPE	   0x0000001F
#define ENT_ID_LEVEL   0x000007E0
#define ENT_ID_INDEX   0x0001F800
#define ENT_ID_LEVEL_S 5
#define ENT_ID_INDEX_S 11

#define ENT_LOADED_FLAG		0x80000000
#define ENT_PERSISTENT_FLAG 0x40000000
#define ENT_ACTIVE_FLAG		0x20000000
#define ENT_VISIBLE_FLAG	0x10000000
#define ENT_DETECT_FLAG		0x08000000
#define ENT_COLLIDE_FLAG	0x04000000

#define LOAD_ENTITY(name, i)           \
	entity_inits[i]	 = &name##_init;   \
	entity_update[i] = &name##_update; \
	entity_render[i] = &name##_render

extern unsigned int max_entities;

extern int (*entity_inits[32])(unsigned int actor_index, unsigned char* data, unsigned char* is_loading);
extern Entity entities[ENTITY_LIMIT];
extern void (*entity_update[32])(unsigned int index);
extern void (*entity_render[32])(unsigned int index);

void unload_entity(Entity* ent);