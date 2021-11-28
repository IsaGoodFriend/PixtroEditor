#pragma once
// Intellisense code for vscode from https://github.com/JamieDStewart/GBA_VSCode_Basic

#ifndef __INTELLISENSE_H__
#define __INTELLISENSE_H__

#if __INTELLISENSE__
#define __attribute__(q)
#define __builtin_strcmp(a,b) 0
#define __builtin_strlen(a) 0
#define __builtin_memcpy(a,b) 0
#define __builtin_va_list void*
#define __builtin_va_start(a,b)
#define __extension__
#endif

#include "tonc_types.h"
#include "tonc_memmap.h"
#include "tonc_memdef.h"

#include "tonc_bios.h"
#include "tonc_core.h"
#include "tonc_input.h"
#include "tonc_irq.h"
#include "tonc_math.h"
#include "tonc_oam.h"
#include "tonc_tte.h"
#include "tonc_video.h"
#include "tonc_surface.h"

#include "tonc_nocash.h"

// For old times' sake
#include "tonc_text.h"

#endif //__INTELLISENSE_H__