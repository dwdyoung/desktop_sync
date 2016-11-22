








#include "config.h"

#ifndef _ENCODER_H_
#define _ENCODER_H_

typedef void (__stdcall *EncoderCallback)(uint8_t* data, int length);

int encoder_init();

int encoder_setEncoderCallback(EncoderCallback callback);

int encoder_tryToMakeFrame();

int encoder_close();

#endif