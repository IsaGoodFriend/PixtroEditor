#pragma once

#define PGRAVITY_LIGHT		0
#define PGRAVITY_HEAVY		1
#define PGRAVITY_REVERSE	2
#define PGRAVITY_NONE		3

// Life time			(4 bits)
// Default Life Time 	(4 bits)
// X vel				(8 bits  (X.X))
// X Coor				(16 bits (XXX.X))

// Animation frame		(2 bits)
// Priority				(2 bits)
// Color index			(4 bits)
// Y vel				(8 bits  (X.X))
// Y Coor				(16 bits (XXX.X))

// Flip Data			(2 bits)
// Gravity				(2 bits)
// Particle Frame start	(8 bits)

void add_particle_basic(int x, int y, int particle, int frame_time, int pal, int priority);
