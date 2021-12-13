#pragma once

#include "core.h"

typedef struct StateMachine
{
	unsigned int state;

	unsigned int (**updates)();
	void (**begin_state)(int old_state);
	void (**end_state)(int new_state);
} StateMachine;

void init_statemachine(StateMachine *machine, int count);

void set_update_state(StateMachine *machine, unsigned int (*function)(), int state_idx);
void set_begin_state(StateMachine *machine, void (*function)(int), int state_idx);
void set_end_state(StateMachine *machine, void (*function)(int), int state_idx);

void set_statemachine(StateMachine *machine, int state);

void update_statemachine(StateMachine *machine);

#define SET_STATE_MACHINE(machine, state, name)    \
	set_update(machine, name##_update, state);     \
	set_begin_state(machine, name##_begin, state); \
	set_end_state(machine, name##_end, state);
