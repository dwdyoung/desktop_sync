






#include "config.h"
#include "decoder.h"
#include "encoder.h"

bool is_init = false;

EXTERN_C FFMPEG_API int STDCALL test(){
	printf("hello jna");
	decoder_testCallback();
	return 1;
}


EXTERN_C FFMPEG_API int STDCALL initDecoder(){
	decoder_init();
	return 1;
}


EXTERN_C FFMPEG_API int STDCALL initEncoder(){
	encoder_init();
	return 1;
}


EXTERN_C FFMPEG_API int pushH264(uint8_t* h264Data, int length){
	return decoder_pushH264(h264Data, length);
}


EXTERN_C FFMPEG_API int tryToMakeFrame(){
	return encoder_tryToMakeFrame();
}



EXTERN_C FFMPEG_API int setEncoderCallback(EncoderCallback callback){
	return encoder_setEncoderCallback(callback);
}



EXTERN_C FFMPEG_API int setDecoderRgbCallback(DecoderCallback callback){
	return decoder_setDecodeRgbCallback(callback);
}



EXTERN_C FFMPEG_API int closeDecoder(){
	return decoder_close();
}


EXTERN_C FFMPEG_API int closeEncoder(){
	return encoder_close();
}


void testCallback(uint8_t** data, int* linesize, int linesizeLength, int width, int height){

}

#if TEST_DEOCDE
FILE* pFin;
int uDataSize;
uint8_t inbuf[4096];
uint8_t* frameBuf;
int frameLength = 0;
uint8_t * tempBuf;
int open_input_output_file(){
	const char* inputFileName = "../easypush.h264";
	fopen_s(&pFin, inputFileName, "rb+");
	if(!pFin){
		printf("Error, open input file failed\n");
		return -1;
	}
	
	return 0;
}


bool checkHead(const uint8_t* buffer, int offset)
{
    // 00 00 00 01
    if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
            && buffer[offset + 2] == 0x00 && buffer[3] == 0x01)
        return true;
    // 00 00 01
    if (buffer[offset] == 0x00 && buffer[offset + 1] == 0x00
            && buffer[offset + 2] == 0x01)
        return true;
    return false;
}

int findHead(const uint8_t* buffer, int offset, int len) {
    int i;
    if (len == 0)
        return -1;
    for (i = offset; i < len; i++)
    {
        if (checkHead(buffer, i))
            return i;
    }
    return -1;
}
#endif


int main(char* argv, int argc){
#if TEST_DEOCDE
	initFfmpeg();
	DecoderCallback callback = (DecoderCallback)testCallback;
	decoder_setDecodeRgbCallback(callback);
	open_input_output_file();
	frameBuf = new uint8_t[409600];
	//¶ÁÈ¡h264
	while(true){
		uDataSize = fread_s(inbuf, 4096, 1, 4096, pFin);
		if(uDataSize == 0){
			break;
		}

		memcpy(&frameBuf[frameLength], inbuf, uDataSize);
		frameLength += uDataSize;

		int head = findHead(frameBuf, 4, frameLength - 4);
		while(head > 0){
			uint8_t* h264Buf = new uint8_t[head];
			memcpy(h264Buf, frameBuf, head);
			frameLength -= head;
			decoder_pushH264(h264Buf, head);
			free(h264Buf);

			uint8_t* tempFrame = new uint8_t[409600];
			memcpy(tempFrame, frameBuf + head, frameLength);
			free(frameBuf);
			frameBuf = tempFrame;

			head = findHead(frameBuf, 4, frameLength - 4);
		}
	}

	decoder_close();
#else
	encoder_init();
	for(int i = 0; i < 100; i++){
		tryToMakeFrame();
	}
	encoder_close();

#endif




	return 1;
}

