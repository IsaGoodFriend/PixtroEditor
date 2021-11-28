#pragma once

#define FIXED2INT(n)			((n) >> ACC)
#define INT2FIXED(n)			((n) << ACC)

#define FIXED_DIV(a, b)			(INT2FIXED(a) / (b))
#define FIXED_MULT(a, b)		(FIXED2INT((a) * (b)))

#define INT_ABS(n)				((n) * (((n)>>31) | 1))
#define INT_SIGN(n)				(((n) != 0) * (((n)>>31) | 1))
#define INT_SIGNED(n, s)		(((n) != 0) * (((n)>>31) | 1))
#define INT_LERP(a, b, t)		((((a) * (0x100 - (t))) + ((b) * (t))) >> ACC)
#define FIXED_LERP(a, b, t)		(FIXED_MULT((b), (t)) + FIXED_MULT((a), 0x100 - (t)))
#define FIXED_APPROACH(a, b, m)	((a > b) * SIGNED_MAX(a - m, b) + (a <= b) * SIGNED_MIN(a + m, b))

#define SIGNED_MAX(a, b)		(b + (a - b) * (a > b))
#define SIGNED_MIN(a, b)		(b + (a - b) * (a < b))

#define SET_VAL_IF(og, val, b) 	((og) * !(b) | (val) * (b))
#define RESET_IF(val, b)		val &= ~(0xFFFFFFFF * (b))

#define COLOR_LERP(a, b, t) ((INT_LERP(a, b, t) & 0x7C00) | (INT_LERP((a & 0x03E0), (b & 0x03E0), t) & 0x03E0) | (INT_LERP((a & 0x001F), (b & 0x001F), t)) )

#define ACC			8

#define TRANSLATE_MATRIX(m, x, y)	m = matrix_multiply(m, matrix_trans(x, y))
#define ROTATE_MATRIX(m, r)			m = matrix_multiply(m, matrix_rot(r))
#define SCALE_MATRIX(m, s)			m = matrix_multiply(m, matrix_scale(s, s))
#define SCALE_MATRIX_XY(m, x, y)	m = matrix_multiply(m, matrix_scale(x, y))

typedef struct AffineMatrix {
	int values[6];
	
} AffineMatrix;

AffineMatrix matrix_multiply(AffineMatrix a, AffineMatrix b);

AffineMatrix matrix_identity();
AffineMatrix matrix_trans(int x, int y);
AffineMatrix matrix_rot(int rot);
AffineMatrix matrix_scale(int scale_x, int scale_y);

int fixed_sqrt(int x);
unsigned int RNG();
