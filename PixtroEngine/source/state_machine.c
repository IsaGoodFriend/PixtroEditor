#include "state_machine.h"
#include <stdlib.h>
#include "pixtro_basic.h"

void init_statemachine(StateMachine *machine, int count)
{
	if (machine->updates)
		free(machine->updates);
	if (machine->begin_state)
		free(machine->begin_state);
	if (machine->end_state)
		free(machine->end_state);

	machine->state = 0;

	machine->updates = (unsigned int (**)(int))malloc(4 * count);
	machine->begin_state = (void (**)(int, int))malloc(4 * count);
	machine->end_state = (void (**)(int, int))malloc(4 * count);
}

void set_update(StateMachine *machine, unsigned int (*function)(int), int state_idx)
{
	machine->updates[state_idx] = function;
}
void set_begin_state(StateMachine *machine, void (*function)(int, int), int state_idx)
{
	machine->begin_state[state_idx] = function;
}
void set_end_state(StateMachine *machine, void (*function)(int, int), int state_idx)
{
	machine->end_state[state_idx] = function;
}

void set_statemachine(StateMachine *machine, int state, int entity_index)
{
	if (state != machine->state)
	{
		if (machine->end_state[machine->state])
			machine->end_state[machine->state](entity_index, state);

		unsigned int old_state = machine->state;
		machine->state = state;

		if (machine->begin_state[machine->state])
			machine->begin_state[machine->state](entity_index, old_state);
	}
}

void update_statemachine(StateMachine *machine, int entity_index)
{
	unsigned int new_state = machine->updates[machine->state](entity_index);
	if (new_state == -1)
		return;

	set_statemachine(machine, new_state, entity_index);
}