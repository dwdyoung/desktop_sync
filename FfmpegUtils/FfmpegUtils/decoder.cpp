








#include "decoder.h"

AVCodec *pCodec = NULL;
AVCodecContext *pCodecContext = NULL;
AVCodecParserContext *pCodecParserCtx = NULL;
AVFrame *frame = NULL;
AVFrame *rgbFrame = NULL;
SwsContext* img_convert_ctx = NULL;
AVPacket pkt;
int got_frame = 0;
DecoderCallback decoder_rgb_callback = NULL;

int decoder_init(){
	avcodec_register_all();
	av_init_packet(&pkt);
	pCodec = avcodec_find_decoder(AV_CODEC_ID_H264);
	if(!pCodec){
		printf("Error, find codec failed.\n");
		return -1;
	}

	pCodecContext = avcodec_alloc_context3(pCodec);
	if(!pCodecContext){
		printf("Error, alloc codec context failed.\n");
		return -1;
	}

	//if(pCodec->capabilities & AV_CODEC_CAP_TRUNCATED){
	//	pCodecContext->flags |= AV_CODEC_CAP_TRUNCATED;
	//}

	pCodecParserCtx = av_parser_init(AV_CODEC_ID_H264);
	if(!pCodecParserCtx){
		printf("Error, alloc parse failed.\n");
		return -1;
	}

	if(avcodec_open2(pCodecContext, pCodec, NULL) < 0){
		printf("Error: Opening codec failed\n");
		return -1;
	}

	frame = av_frame_alloc();
	rgbFrame = avcodec_alloc_frame();  
	if(!frame || !rgbFrame){
		printf("Error: Alloc frame failed.\n");
		return -1;
	}

	

	return 0;
}



int decoder_close(){

	// Ý”³öÊ£ðNµÄ”µ“þ
	pkt.data = NULL;
	pkt.size = NULL;
	while(true){
		int ret = avcodec_decode_video2(pCodecContext, frame, &got_frame, &pkt);
		if(ret < 0){
			printf("Error: decode failed.\n");
			return -1;
		}

		if(got_frame){
			printf("Decoded 1 frame OK!\n");
			
		} else {
			break;
		}
	}

	sws_freeContext(img_convert_ctx);   
	img_convert_ctx = NULL;
	avcodec_close(pCodecContext);
	av_free(pCodecContext);
	pCodecContext = NULL;
	av_frame_free(&frame);
	frame = NULL;
	av_frame_free(&rgbFrame);
	rgbFrame = NULL;
	return 1;
}



int decoder_setDecodeRgbCallback(DecoderCallback callback){
	decoder_rgb_callback = callback;
	return 1;
}


int decoder_pushH264(uint8_t* h264Data, int length){
	int len = av_parser_parse2(pCodecParserCtx, pCodecContext, &pkt.data, 
				&pkt.size, h264Data, length, AV_NOPTS_VALUE, AV_NOPTS_VALUE, AV_NOPTS_VALUE);
	
	int ret = avcodec_decode_video2(pCodecContext, frame, &got_frame, &pkt);
	if(ret < 0){
		printf("Error: decode failed.\n");
		return -1;
	}

	if(got_frame > 0){

		printf("Decoded 1 frame OK!\n");
		
		if(NULL == img_convert_ctx)
		{
			uint8_t *out_buffer;  
			out_buffer = new uint8_t[avpicture_get_size(AV_PIX_FMT_RGB24, frame->width, frame->height)];  
			if(!avpicture_fill((AVPicture *)rgbFrame, out_buffer, AV_PIX_FMT_RGB24, frame->width, frame->height)){
				printf("Error: Alloc frame failed.\n");
				return -1;
			}


			img_convert_ctx = sws_getContext(
				frame->width, frame->height,
				AV_PIX_FMT_YUV420P, 
				frame->width, frame->height,
				AV_PIX_FMT_RGB24, 
				SWS_BICUBIC, NULL, NULL, NULL);   

			rgbFrame->width = frame->width;
			rgbFrame->height = frame->height;
			rgbFrame->format = AV_PIX_FMT_RGB24;

		}

		if(NULL != decoder_rgb_callback)
		{
		// Êä³örgb
			sws_scale(img_convert_ctx, 
				(const uint8_t* const*)frame->data, 
				frame->linesize, 
				0, 
				frame->height, 
				rgbFrame->data, 
				rgbFrame->linesize); 

			decoder_rgb_callback(
				rgbFrame->data, 
				rgbFrame->linesize, 
				GET_ARRAY_LEN(rgbFrame->linesize), 
				rgbFrame->width, 
				rgbFrame->height);
		}
		
		return 1;
	} else {
		return 0;
	}
}



int decoder_testCallback(){
	
	return 1;
}