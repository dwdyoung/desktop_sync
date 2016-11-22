







#include "config.h"

#ifndef _DECODER_H_
#define _DECODER_H_

typedef void (__stdcall *DecoderCallback)(uint8_t** data, int* linesize, int linesizeLength, int width, int height);

int decoder_init();

int decoder_pushH264(uint8_t* h264Data, int length);

//int decoder_setDecodeCallback(DecoderCallback callback);

int decoder_setDecodeRgbCallback(DecoderCallback callback);

int decoder_close();

int decoder_testCallback();



#endif