#pragma once

#include <string.h>

// ---- CREDITS ----
//
// Taken from Noel Berry and modified for use in the GBA
// https://gist.github.com/NoelFB/7a5fa66fc29dd7ed1c11042c30f1b00e
//
// =================

// Holds the state of a Coroutine
typedef struct {
	// Current "waiting time" before we run the next block
	int wait_for;

	// Used during `rt_for`, which repeats the given block for X time
	int repeat_for;

	// Current block of the routine
	int at;
} Routine;
typedef struct {
	void (*function)(Routine*);
} RoutineFunction;


#define reset_routine(routine)	\
	routine.at = 0;				\
	routine.repeat_for = 0;		\
	routine.wait_for = 0;


// Control Statements:
// These must be used in the same scope (ex. you can't put
// control statements inside an if statement or a while loop.
// Only one rt_begin and rt_end may be used within a single scope,
// as they use a goto to control flow that will clash.

// Begins the Coroutine, location 0
// `routine` is a reference to a Routine struct which holds the state /* routine ref */ /* move-next */
#define rt_begin(routine)						\
    if (routine.wait_for)						\
        routine.wait_for--;						\
    else										\
    {											\
        Routine *__rt = &routine;				\
        int __mn = 1;							\
        switch (__rt->at)						\
        {										\
        case 0: {								\

// Waits until the next frame to begin the following block
#define rt_step()								\
            if (__mn) __rt->at = __LINE__;		\
            } break;							\
        case __LINE__: {						\

// Same as `rt_step` but can be jumped
// to by using rt_goto(value)
// Can be any positive 1-4 digit hex number.  Labels of different digit lengths will be considered unique
#define rt_label(value)							\
            if (__mn) {							\
				__rt->at = 0x4000##value;		\
			goto rt_label_##value; }			\
            } break;							\
        case 0x4000##value: {					\
			rt_label_##value:

// Repeats the following block for the given amount of frames
#define rt_for(time)							\
        rt_step();								\
        if (__rt->repeat_for < time)    {		\
            __rt->repeat_for++;					\
            __mn = 0;							\
        }										\
        else __rt->repeat_for = 0;				\

// Repeats the following block while the condition is met
#define rt_while(condition)						\
        rt_step();								\
        if (condition)							\
            __mn = 0;							\

// Waits a given amount of time before beginning the following block
#define rt_wait(time)							\
            if (__mn) __rt->wait_for = time;	\
            rt_step();							\

// Ends the Coroutine
#define rt_end()								\
            if (__mn) __rt->at = -1;			\
            } break;							\
        }										\
    }											\
    rt_end_of_routine:							\

// Flow Statements:
// These can be used anywhere between rt_begin and rt_end,
// including if statements, while loops, etc.

// Repeats the block that this is contained within
// Skips the remainder of the block
#define rt_repeat()								\
        goto rt_end_of_routine					\

// Goes to a given block labeled with `rt_label`
#define rt_goto(value)							\
        do {									\
            __rt->at = 0x4000##value;			\
            goto rt_end_of_routine;				\
        } while(0)								\

// Goes to a given block labeled with `rt_label`
#define rt_goto_now(value)						\
        do {									\
            __rt->at = 0x4000##value;			\
            goto rt_label_##value;				\
        } while(0)								\

// Restarts the entire Coroutine;
// Jumps back to `rt_begin` on the next frame
#define rt_restart()							\
        do {									\
            __rt->at = 0;						\
            __rt->wait_for = 0;					\
            __rt->repeat_for = 0;				\
            goto rt_end_of_routine;				\
        } while(0)								\

// Example:
//
// // Assuming you have a `routine` variable stored somewhere
// rt_begin(routine);
// {
//     // stuff that happens frame 1
// }
// rt_wait(1.0f);
// {
//     // after 1.0s, this block runs
// }
// rt_for(0.25f);
// {
//     // this block repeats for 0.25s
// }
// rt_step();
// {
//     // the following frame, this block is run
// }
// rt_label("ABC");
// {
//     if (something)
//         rt_repeat();
//
//     // not run if rt_repeat() was called
//     something_else();
// }
// rt_step();
// {
//     if (another) rt_goto("ABC"); // jumps to "ABC"
//     if (another2) rt_restart();  // jumps to rt_begin
//     // otherwise the next block will be run next frame
// }
// rt_while(condition_is_true);
// {
//     // this is repeated until condition_is_true is false
// }
// rt_end();
//