#include "character.h"
#include "pixtro_basic.h"
#include "state_machine.h"

#define CHAR_WIDTH 12

#define CHAR_HEIGHT 24
#define CHAR_R_HEIGHT 12
#define HEIGHT_DIFF 12

#define JUMP_HEIGHT -0x400
#define JUMP_ROLL_HEIGHT -0x3C0
#define GRAVITY 0x38
#define MAX_FALL 0x600

#define ROLL_SPEED 0x500
#define ROLL_JUMP -0x0C0

#define HAS_FLAG(name, value) (name##_FLAG & value)
#define SET_FLAG(name, value) value |= name##_FLAG;
#define CLEAR_FLAG(name, value) value &= ~name##_FLAG;

// flag 0 data
#define GROUND_FLAG 0x0001
#define HOLDJUMP_FLAG 0x0002
#define ROLLJUMP_FLAG 0x0004

// State definitions
#define STATE_NORMAL 0
#define STATE_ROLLING 1

StateMachine char_machine;

int roll_angle;

void roll_begin(int, int);
void roll_end(int, int);
int normal_update(int);
int roll_update(int);

int character_init(unsigned int actor_index, unsigned char *data, unsigned char *is_loading)
{
	entities[actor_index].width = CHAR_WIDTH;
	entities[actor_index].height = CHAR_HEIGHT;

	init_statemachine(&char_machine, 1);
	set_update(&char_machine, &normal_update, 0);

	return 0;
}

int normal_update(int index)
{
	Entity *ent = &entities[index];

	if (INT_ABS(ent->vel_x) <= 0x200)
		CLEAR_FLAG(ROLLJUMP, ent->flags[0]);

	if (HAS_FLAG(ROLLJUMP, ent->flags[0]))
	{
		ent->vel_y = FIXED_APPROACH(ent->vel_y, MAX_FALL, (GRAVITY / 2));
	}
	else
	{
		ent->vel_y = FIXED_APPROACH(ent->vel_y, MAX_FALL, GRAVITY);
	}

	if (key_pressed(KEY_A, 5))
	{
		ent->vel_y = JUMP_HEIGHT;
		SET_FLAG(HOLDJUMP, ent->flags[0]);

		clear_buffer(KEY_A);
	}

	if (HAS_FLAG(GROUND, ent->flags[0]))
	{
		ent->vel_x = FIXED_APPROACH(ent->vel_x, (key_tri_horz() * 0x200), 0x80);
	}
	else
	{
		if (INT_ABS(ent->vel_x) <= 0x200)
			ent->vel_x = FIXED_APPROACH(ent->vel_x, (key_tri_horz() * 0x200), 0x48);
		else
			ent->vel_x = FIXED_APPROACH(ent->vel_x, (key_tri_horz() * 0x200), 0x28);

		if (ent->vel_y > 0)
			CLEAR_FLAG(HOLDJUMP, ent->flags[0]);

		if (HAS_FLAG(HOLDJUMP, ent->flags[0]) && !KEY_DOWN_NOW(KEY_A))
		{
			ent->vel_y = FIXED_APPROACH(ent->vel_y, 0, 0x100);
		}
	}

	return char_machine.state;
}

void character_update(int index)
{
	Entity *ent = &entities[index];

	update_statemachine(&char_machine, index);

	int vel_y_prev = ent->vel_y;

	unsigned int hit_values = entity_physics(ent, 0x1);

	if (vel_y_prev > 0 && !ent->vel_y)
	{
		SET_FLAG(GROUND, ent->flags[0]);
		CLEAR_FLAG(HOLDJUMP, ent->flags[0]);
	}
	else
	{
		CLEAR_FLAG(GROUND, ent->flags[0]);
	}
}
void character_render(int index)
{
	Entity ent = entities[index];
	draw(ent.x - 0xA00, ent.y - 0x800, 0, 0, 0, 0);
}