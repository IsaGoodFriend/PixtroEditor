
.text

	.code	16
	.align	2
	.thumb_func
@void lz10_decmp_WRAM(byte *source, byte *dest)
lz10_decmp_WRAM:
	swi		0x11
	bx		lr

	.globl	load_header
	.code	32
@ void load_header(unsigned char* ptr)
@ r0 is always the level data pointer
load_header:
	
	ldr		r1, =level_toload
	ldr		r1, [r1]
	ldr		r3, =loading_width
	ldrh	r2, [r0]
	strh	r2, [r1]
	str		r2, [r3]
	ldrh	r2, [r0, #2]
	strh	r2, [r1, #2]
	ldr		r3, =loading_height
	str		r2, [r3]	@ load width and height into level storage
	
	add		r0,	#4
	add		r1,	#4
	
.ld_meta:
	ldr		r2,	[r0]		@ load meta index
	
	cmp		r2, #128
	bge		.ld_meta_e		@ skip if >= 128
	
	ldr		r3,	[r0, #1]	@ load meta value
	strb	r2, [r1]
	strb	r3,	[r1, #1]	@ save meta value
	add		r1, #2
	
	add		r0,	#2			@ move to next meta index value
	b		.ld_meta
.ld_meta_e:
	add		r0, #1
	
	mov		r2, #255
	lsl		r2, #8
	orr		r2, #255
	strh	r2, [r1]
	add		r1, #2
	
	@ --- end of loading ---
	ldr		r2, =level_toload
	str		r1, [r2]
	
	ldr		r1, =lvl_info
	str		r0, [r1]
	bx		lr
@ end load_header

@void fade_black(int factor) factor = 0-5
@ r0 = factor
	.align 4
	.globl fade_black
	.code 32
fade_black:
	
	@ if fade factor is zero, then set color value to default values
	cmp		r0, #0
	beq		.fade_allcolor
	@ if fade factor is 5 or greater, make screen fully black
	cmp		r0, #5
	bge		.fade_allblack
	
	push	{lr}
	
	@ lr = mask
	mov		r2, #0
	mov		r1, r0
.fade_addloop:
	lsl		r2, #1
	add		r2, #1
	subs	r1, #1
	bne		.fade_addloop
	
	@ r1 = palette pointer
	mov		r1, #5
	lsl		r1, #24
	
	mov		r3, #5		@ lr <<= 5 - r0
	sub		r3, r3, r0
	lsl		r2, r3
	
	mov		lr, #1
	lsl		lr, #10
	orr		lr, r2
	lsl		lr, #5
	orr		lr, r2
	
	@ counter for 
	mov		r3, #512
	
	@ r2 = color_bank
	ldr		r2, =colorbank
	
.fade_forloop:
	
	push	{r3}
	ldrh	r3, [r2]
	lsr		r3,	r0
	bic		r3, lr
	strh	r3, [r1]
	
	add		r1, #2
	add		r2, #2
	
	pop		{r3}
	subs	r3, #1
	bne		.fade_forloop
	
	pop		{lr}
	bx		lr


.fade_allblack:
	mov		r0, #5
	lsl		r0, #24

	mov		r1, #0
	
	mov		r2, #256
.fadeab_forloop:
	str		r1, [r0]
	add		r0, #4
	
	subs	r2, #1
	bne		.fadeab_forloop
	
	bx		lr
	
	
.fade_allcolor:
	@ r0 = palette pointer
	mov		r0, #5
	lsl		r0, #24

	@ r1 = color_bank
	ldr		r1, =colorbank
	
	mov		r2, #256
.fadeac_forloop:
	ldr		r3, [r1]
	str		r3, [r0]
	add		r0, #4
	add		r1, #4
	
	subs	r2, #1
	bne		.fadeac_forloop
	
	bx		lr