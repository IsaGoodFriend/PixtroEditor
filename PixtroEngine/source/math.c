#include "math.h"

unsigned int s1 = 0x1234, s2 = 0x4567, s3 = 0x89AB;

const int sine_table[91] = { 
	0x000, 0x004, 0x009, 0x00D, 0x012, 0x016, 0x01B, 0x01F, 0x024, 0x028, 0x02C, 0x031, 0x035, 0x03A, 0x03E, 0x042, 
	0x047, 0x04B, 0x04F, 0x053, 0x058, 0x05C, 0x060, 0x064, 0x068, 0x06C, 0x070, 0x074, 0x078, 0x07C, 0x080, 0x084, 
	0x088, 0x08B, 0x08F, 0x093, 0x096, 0x09A, 0x09E, 0x0A1, 0x0A5, 0x0A8, 0x0AB, 0x0AF, 0x0B2, 0x0B5, 0x0B8, 0x0BB, 
	0x0BE, 0x0C1, 0x0C4, 0x0C7, 0x0CA, 0x0CC, 0x0CF, 0x0D2, 0x0D4, 0x0D7, 0x0D9, 0x0DB, 0x0DE, 0x0E0, 0x0E2, 0x0E4, 
	0x0E6, 0x0E8, 0x0EA, 0x0EC, 0x0ED, 0x0EF, 0x0F1, 0x0F2, 0x0F3, 0x0F5, 0x0F6, 0x0F7, 0x0F8, 0x0F9, 0x0FA, 0x0FB, 
	0x0FC, 0x0FD, 0x0FE, 0x0FE, 0x0FF, 0x0FF, 0x0FF, 0x100, 0x100, 0x100, 0x100, 
};

int int_deg_sin(int angle) {
	while (angle < 0)
		angle += 360;
	while (angle >= 360)
		angle -= 360;
	
	if (angle < 90) {
		return sine_table[angle];
	}
	else if (angle < 180) {
		return sine_table[180 - angle];
	}
	else if (angle < 270) {
		return -sine_table[angle - 180];
	}
	else {
		return -sine_table[360 - angle];
	}
}
int int_deg_cos(int angle) {
	
	return int_deg_sin(angle + 90);
}

int fixed_sqrt(int x) {
    unsigned int t, q, b, r;
    r = x;
    b = 0x1000;
    q = 0;
    while( b > 0x0 )
    {
        t = q + b;
		x = r >= t;
		
		r -= t * x;
		q *= !x;
		q += ((t + b) * x); // equivalent to q += 2*b
        
        r <<= 1;
        b >>= 1;
    }
    q >>= 3;
    return (int)q;
}

AffineMatrix matrix_multiply(AffineMatrix b, AffineMatrix a) {
	int x, y, i;
	
	AffineMatrix ret;
	
	int b_values[9];
	
	for (x = 0; x < 3; ++x)
		for (y = 0; y < 2; ++y)
			b_values[x + (y * 3)] = b.values[x + (y * 3)];
	
	b_values[6] = 0;
	b_values[7] = 0;
	b_values[8] = 0x100;
	
	for (x = 0; x < 3; ++x)
		for (y = 0; y < 6; y += 3) {
			ret.values[x + y] = 0;
			for (i = 0; i < 3; ++i)
				ret.values[x + y] += FIXED_MULT(a.values[i + y], b_values[x + (i * 3)]);
		}
		
	return ret;
}
AffineMatrix matrix_identity() {
	AffineMatrix mat;
	
	int i;
	
	for (i = 0; i < 6; ++i)
		mat.values[i] = 0;
	
	mat.values[0] = 0x100;
	mat.values[4] = 0x100;
	
	return mat;
}
AffineMatrix matrix_trans(int x, int y) {
	AffineMatrix mat = matrix_identity();
	
	mat.values[2] = x;
	mat.values[5] = y;
	
	return mat;
}
AffineMatrix matrix_trans_int(int x, int y) {
	AffineMatrix mat = matrix_identity();
	
	mat.values[2] = INT2FIXED(x);
	mat.values[5] = INT2FIXED(y);
	
	return mat;
}
AffineMatrix matrix_rot(int rot) {
	AffineMatrix mat = matrix_identity();
	
	mat.values[0] =  int_deg_cos(rot);
	mat.values[1] = -int_deg_sin(rot);
	mat.values[3] =  int_deg_sin(rot);
	mat.values[4] =  int_deg_cos(rot);
	
	return mat;
}
AffineMatrix matrix_scale(int scale_x, int scale_y) {
	AffineMatrix mat = matrix_identity();
	
	mat.values[0] = scale_x;
	mat.values[4] = scale_y;
	
	return mat;
}

unsigned int RNG() {
	s1 = (50000 * s1) % 715827817;
	s2 = (500001 * s2) % 715827821;
	s3 = (6100003 * s3) % 715827829;
	return s1 + s2 + s3;
}
void rng_seed(unsigned int seed1, unsigned int seed2, unsigned int seed3) {
	s1 = seed1;
	s2 = seed2;
	s3 = seed3;
}
