#include "tonc_vscode.h"
#include <string.h>

#include "core.h"
#include "math.h"
#include "particles.h"
#include "sprites.h"

// Life time			(8 bits)
// X vel				(8 bits  (X.X))
// X Coor				(16 bits (XXX.X))
#define ATT1_LIFE	0xFF000000
#define ATT1_LIFE_1 0x01000000

// Animation frame		(4 bits)
// Color index			(4 bits)
// Y vel				(8 bits  (X.X))
// Y Coor				(16 bits (XXX.X))
#define ATT2_ANIMFRAME		0xF0000000
#define ATT2_ANIMFRAME_TICK 0x10000000
#define ATT2_PAL			0x0F000000
#define ATT2_PAL_S			24

// Default Life Time 	(8 bits)
// ???
// Gravity				(2 bits)
// Priority				(2 bits)
// Flip Data			(2 bits)
// Particle Frame start	(8 bits)
#define ATT3_DEF_LIFE		0xFF000000
#define ATT3_DEF_LIFE_SHIFT 0
#define ATT3_GRAV_1			0x00010000
#define ATT3_GRAV_S			0x00020000
#define ATT3_PRIO			0x0000C000
#define ATT3_PRIO_S			12
#define ATT3_FLIP			0x00003000
#define ATT3_START_FRAME	0x00000FFF

#define PARTICLE_MAX	   288 // 96 * 3
#define PARTICLE_DATA_SIZE 3

unsigned int particle_data[PARTICLE_MAX];
int lastParticle;

#define PARTICLE_FRAME(i) (((particle_data[i + 1] & ATT2_ANIMFRAME) >> 28) + (particle_data[i + 2] & ATT3_START_FRAME)) << 3

void add_particle(int att1, int att2, int att3);

extern const int particles[];

extern int sprite_count;
extern OBJ_ATTR* sprite_pointer;
extern int cam_x, cam_y;

void add_particle_basic(int x, int y, int particle, int frame_time, int pal, int priority) {

	if (!frame_time || !particle)
		return;

	int rng = RNG();

	x <<= 4;
	y <<= 4;

	x |= frame_time << 24;
	int vel = rng & 0x7F;
	vel /= 5;
	if (rng & 0x80)
		vel = ~vel;
	rng >>= 8;
	x |= (vel << 16) & 0xFF0000;

	y |= (particle & 0xF000) << 16;
	vel = rng & 0x7F;
	vel /= 5;
	if (rng & 0x80)
		vel = ~vel;
	rng >>= 8;
	y |= (vel << 16) & 0xFF0000;

	particle &= ATT3_START_FRAME;
	particle |= priority << ATT3_PRIO_S;
	particle |= frame_time << 24;

	add_particle(x, y, particle);
}

void add_particle(int att1, int att2, int att3) {

	particle_data[lastParticle]		= att1;
	particle_data[lastParticle + 1] = att2;
	particle_data[lastParticle + 2] = att3;

	memcpy(&tile_mem[4][lastParticle / 3], &particles[PARTICLE_FRAME(lastParticle)], 32);

	lastParticle += 3;
	lastParticle %= PARTICLE_MAX;
}

void update_particles() {
	int index, moveParticles = 1;
	int sp = 0;

#ifdef __DEBUG__
	if (ENGINE_DEBUGFLAG(PAUSE_UPDATES))
		moveParticles = 0;
#endif

	for (index = 0; index < PARTICLE_MAX; index += PARTICLE_DATA_SIZE) {
		if (!(particle_data[index] & ATT1_LIFE)) {
			if (particle_data[index + 1] & ATT2_ANIMFRAME) {
				particle_data[index + 1] -= ATT2_ANIMFRAME_TICK;
				particle_data[index] |= ((particle_data[index + 2] & ATT3_DEF_LIFE));

				memcpy(&tile_mem[4][index / 3], &particles[PARTICLE_FRAME(index)], 32);
			} else
				continue;
		}

		particle_data[index] -= ATT1_LIFE_1 * moveParticles;

		// X Velocity
		int pos = particle_data[index] & 0xFFFF;
		int vel = ((particle_data[index] & 0x7F0000) >> 16) | ((particle_data[index] & 0x800000) ? 0xFFFFFF80 : 0);
		pos += vel * moveParticles;
		pos &= 0xFFFF;
		particle_data[index] = pos | (particle_data[index] & 0xFFFF0000);

		pos = (pos >> 4) - cam_x;
		if (pos < -8 || pos > 240) {
			particle_data[index] &= ~ATT1_LIFE;
			continue;
		}

		pos = particle_data[index + 1] & 0xFFFF;
		vel = ((particle_data[index + 1] & 0x7F0000) >> 16) | ((particle_data[index + 1] & 0x800000) ? 0xFFFFFF80 : 0);
		vel += ((((particle_data[index + 2] & ATT3_GRAV_1) >> 8) | (particle_data[index + 2] & ATT3_GRAV_S ? 0xFFFFFFFE : 0)) + 1) * moveParticles;
		pos += vel * moveParticles;
		pos &= 0xFFFF;
		particle_data[index + 1] = pos | (particle_data[index + 1] & 0xFF000000) | ((vel & 0xFF) << 16);

		pos = (pos >> 4) - cam_y;
		if (pos < -8 || pos > 160) {
			particle_data[index] &= ~ATT1_LIFE;
			continue;
		}

		vel = ((particle_data[index] & 0xFFFF) >> 4) - cam_x;

		obj_set_attr((sprite_pointer + (sp++)),
					 ATTR0_SQUARE | ATTR0_Y(pos),																															// ATTR0
					 ATTR1_SIZE_8 | ATTR1_X(vel) | ((particle_data[index + 2] & ATT3_FLIP) << 2),																			// ATTR1
					 ATTR2_PALBANK((particle_data[index + 1] & ATT2_PAL) >> ATT2_PAL_S) | (index / 3) | ATTR2_PRIO((particle_data[index + 2] & ATT3_PRIO) >> ATT3_PRIO_S)); // ATTR2
	}

	sprite_count += sp;
	sprite_pointer += sp;
}
void ClearParticles() {
	int index;
	for (index = 0; index < PARTICLE_MAX; index += PARTICLE_DATA_SIZE) {
		particle_data[index]	 = 0;
		particle_data[index + 1] = 0;
	}
}
