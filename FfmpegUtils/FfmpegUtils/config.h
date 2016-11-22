

#ifndef _CONFIG_H_
#define _CONFIG_H_


#include <stdio.h>
using namespace std;

#define __STDC_CONSTANT_MACROS

#ifdef _WIN32
//Windows
extern "C"
{
#include "libavutil/opt.h"
#include "libavcodec/avcodec.h"
#include "libavformat/avformat.h"
#include "libswscale/swscale.h"
#include "libavdevice/avdevice.h"
};
#else
//Linux...
#ifdef __cplusplus
extern "C"
{
#endif
#include <libavutil/opt.h>
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
#include <libavdevice/avdevice.h>
#include <SDL/SDL.h>
#ifdef __cplusplus
};
#endif
#endif


/**
 * GDI Device Demuxer context
 */
struct gdigrab {
    int        draw_mouse;  /**< Draw mouse cursor (private option) */
};


//#define FFMPEG_API extern "C" _declspec(dllexport) 
#define FFMPEG_API _declspec(dllexport) 
#ifndef EXTERN_C
#define EXTERN_C extern "C"
#endif
#define STDCALL __stdcall



#define GET_ARRAY_LEN(array) ((sizeof(array) / sizeof(array[0])))


#define TEST_DECODE 0

#endif