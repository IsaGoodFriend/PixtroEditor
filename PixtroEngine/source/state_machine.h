#pragma once

#include "core.h"

typedef struct StateMachine {
	unsigned int state;
	
	unsigned int (**updates)	(int index);
	void (**begin_state)		(int index, int old_state);
	void (**end_state)			(int index, int new_state);
} StateMachine;

void init_statemachine(StateMachine *machine, int count);

void set_update(StateMachine *machine,  unsigned int (*function)(int), int state_idx);
void set_begin_state(StateMachine *machine, void (*function)(int, int), int state_idx);
void set_end_state(StateMachine *machine, void (*function)(int, int), int state_idx);

void update_statemachine(StateMachine *machine, int entity_index);