#pragma once

#include "core.h"

typedef struct StateMachine
{
	unsigned int state;

	unsigned int (**updates)(int index);
	void (**begin_state)(int index, int old_state);
	void (**end_state)(int index, int new_state);
} StateMachine;

void init_statemachine(StateMachine *machine, int count);

void set_update(StateMachine *machine, unsigned int (*function)(int), int state_idx);
void set_begin_state(StateMachine *machine, void (*function)(int, int), int state_idx);
void set_end_state(StateMachine *machine, void (*function)(int, int), int state_idx);

void set_statemachine(StateMachine *machine, int state, int entity_index);

void update_statemachine(StateMachine *machine, int entity_index);

#define SET_STATE_MACHINE(machine, state, name)    \
	set_update(machine, name##_update, state);     \
	set_begin_state(machine, name##_begin, state); \
	set_end_state(machine, name##_end, state);

