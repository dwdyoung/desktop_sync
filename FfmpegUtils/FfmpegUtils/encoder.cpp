








#include "encoder.h"

AVFormatContext	*inputFmtCtx = NULL;
AVCodecContext	*inputCodecCtx = NULL;
AVCodec			*inputCodec = NULL;
AVStream		*inputStream = NULL;
AVInputFormat	*inputFormat = NULL;
AVFrame			*pFrame = NULL;
AVFrame			*pFrameYUV = NULL;
int				ret, got_picture;
int				videoindex;
AVPacket		*packet = NULL;	// 用于存放帧数据
SwsContext		*rgb_to_yuv = NULL;
EncoderCallback encoderCallback = NULL;
clock_t			start, finish, last_finish;


AVPacket		outputPct;
bool			hasnot_init_packet = true;
AVCodecContext	*outputCodecCtx = NULL;
AVCodec			*outputCodec = NULL;
bool			first_frame = true;			// 第一帧为sps pps结合的帧, 应该提取出来

int encoder_init(){
	av_register_all();
	avformat_network_init();
	inputFmtCtx = avformat_alloc_context();
	avdevice_register_all();


	// 使用gdigrab截取桌面
	AVDictionary* options = NULL;

	// 打开并检查gdigrab
	inputFormat = av_find_input_format("gdigrab");
	if(avformat_open_input(&inputFmtCtx, "desktop" , inputFormat, &options)!=0){
		printf("Couldn't open input stream.\n");
		return -1;
	}


	gdigrab* pGdi = (gdigrab*)inputFmtCtx->priv_data;
	pGdi->draw_mouse = 0;
	
	// av_dump_format(inputFmtCtx, 0, output_file, 1);

	// 检查
	if(avformat_find_stream_info(inputFmtCtx,NULL)<0) {
		printf("Couldn't find stream information.\n");
		return -1;
	}

	// 获取视频通道号
	videoindex = -1;
	for(int i=0; i<inputFmtCtx->nb_streams; i++) {
		if(inputFmtCtx->streams[i]->codec->codec_type==AVMEDIA_TYPE_VIDEO)
		{
			videoindex=i;
			break;
		}
	}
	if(videoindex==-1)
	{
		printf("Didn't find a video stream.\n");
		return -1;
	}
	inputStream = inputFmtCtx->streams[videoindex];

	// 获取视频流的编码格式
	inputCodecCtx = inputFmtCtx->streams[videoindex]->codec;
	inputCodec = avcodec_find_decoder(inputCodecCtx->codec_id);
	if(inputCodec==NULL)
	{
		printf("Codec not found.\n");
		return -1;
	}

	// 该函数用于初始化一个视音频编解码器的AVCodecContext
	// 解决编码延时问题
	AVDictionary *param2 = NULL;   
	av_dict_set(&param2, "preset", "superfast",   0);  
    av_dict_set(&param2, "tune",   "zerolatency", 0);  
	if(avcodec_open2(inputCodecCtx, inputCodec, &param2) < 0)
	{
		printf("Could not open codec.\n");
		return -1;
	}

	pFrame = av_frame_alloc();
	pFrameYUV = av_frame_alloc();
	packet=(AVPacket *)av_malloc(sizeof(AVPacket));

	


	// 为GBRA转YUV格式做准备
	rgb_to_yuv = sws_getContext(
		inputCodecCtx->width, 
		inputCodecCtx->height, 
		inputCodecCtx->pix_fmt, 
		inputCodecCtx->width, 
		inputCodecCtx->height, 
		AV_PIX_FMT_YUV420P, 
		SWS_BICUBIC, 
		NULL, NULL, NULL);



	///  h264 输出初始化 start

	// 设置必要的参数
	outputCodec = avcodec_find_encoder(AV_CODEC_ID_H264);

	outputCodecCtx = avcodec_alloc_context3(outputCodec);
	outputCodecCtx->codec_id = AV_CODEC_ID_H264;
	outputCodecCtx->codec_type = AVMEDIA_TYPE_VIDEO;
	outputCodecCtx->pix_fmt = AV_PIX_FMT_YUV420P;
	outputCodecCtx->width = inputCodecCtx->width;  
	outputCodecCtx->height = inputCodecCtx->height;
	outputCodecCtx->bit_rate = 4000000;  
	outputCodecCtx->gop_size = 10;		// 多少帧输出一次I帧

	outputCodecCtx->time_base.num = 1;  
	outputCodecCtx->time_base.den = 25;  

	//H264
	outputCodecCtx->me_range = 16;
	outputCodecCtx->max_qdiff = 4;
	outputCodecCtx->qcompress = 0.6;

	outputCodecCtx->qmin = 10;
	outputCodecCtx->qmax = 51;

	//可选参数
	outputCodecCtx->max_b_frames=3;

	av_opt_set(outputCodecCtx->priv_data, "preset", "superfast", 0);  
  
    // 实时编码关键看这句，上面那条无所谓  
    av_opt_set(outputCodecCtx->priv_data, "tune", "zerolatency", 0);

	// 打开编码器
	if(avcodec_open2(outputCodecCtx, outputCodec, NULL) < 0){
		return -1;
	}

	///  h264 输出初始化 end



	start = clock();
	last_finish = clock();
	

	return 1;
}

int encoder_close(){

	// 输出缓存中的图片
	int got_picture2 = 1;
	while(got_picture2 == 1){
		int rets = avcodec_encode_video2(outputCodecCtx, &outputPct, NULL, &got_picture2);
					
		if(rets < 0){
			printf("Failed to encode! \n");
			break; -1;
		}
		if (got_picture2 == 1){

			// pkt相当于一帧的数据， 此处是h264编码
			// int rets = av_write_frame(outputFmtCtx, &outputPct);
			if(NULL != encoderCallback){
				encoderCallback(outputPct.data, outputPct.size);
			}

			av_free_packet(&outputPct);
		}
	}


	av_free(pFrame);
	av_free(pFrameYUV);
	avcodec_close(outputCodecCtx);
	avcodec_close(inputCodecCtx);
	return 1;
}

int encoder_setEncoderCallback(EncoderCallback callback){
	encoderCallback = callback;
	return 1;
}


// 尝试产生一个桌面的h264
int encoder_tryToMakeFrame(){
	if(av_read_frame(inputFmtCtx, packet)>=0) {

		// 检查视频通道是否相同
		if(packet->stream_index == videoindex){
			// 解码一帧视频数据。输入一个压缩编码的结构体AVPacket，输出一个解码后的结构体AVFrame
			int ret2 = avcodec_decode_video2(inputCodecCtx, pFrame, &got_picture, packet);		
			if(ret2 < 0){
				printf("Decode Error.\n");
				return -1;
			}

			if(got_picture){

				if(hasnot_init_packet){
					hasnot_init_packet = false;

					// 根据屏幕大小创建AVPacket
					int picture_size = avpicture_get_size(AV_PIX_FMT_YUV420P, pFrame->width, pFrame->height);
					av_new_packet(&outputPct, picture_size);

					// 根据屏幕大小创建AVFrame
					uint8_t * picture_buf = (uint8_t *)av_malloc(picture_size);
					avpicture_fill((AVPicture *)pFrameYUV, picture_buf, outputCodecCtx->pix_fmt, pFrame->width, pFrame->height);
				}


				// 转成YUV
				pFrameYUV->width = pFrame->width;
				pFrameYUV->height = pFrame->height;
				pFrameYUV->format = AV_PIX_FMT_YUV420P;
				//總共有七個參數； 
				//第一個參數即是由 sws_getContext 所取得的參數。 
				//第二個 src 及第六個 dst 分別指向input 和 output 的 buffer。 
				//第三個 srcStride 及第七個 dstStride 分別指向 input 及 output 的 stride；如果不知道什麼是 stride，姑且可以先把它看成是每一列的 byte 數。 
				//第四個 srcSliceY，就註解的意思來看，是指第一列要處理的位置；這裡我是從頭處理，所以直接填0。想知道更詳細說明的人，可以參考 swscale.h 的註解。 
				//第五個srcSliceH指的是 source slice 的高度。
				int height = sws_scale(
					rgb_to_yuv, 
					(const uint8_t* const*)pFrame->data, 
					pFrame->linesize, 
					0, 
					inputCodecCtx->height, 
					pFrameYUV->data, 
					pFrameYUV->linesize);
				
#if COUNT_TIME
				finish = clock();
				totaltime = (double)(finish - last_finish) / CLOCKS_PER_SEC;
				printf("a frame take %f seconds\n", totaltime);
				last_finish = finish;
#endif

				// 写入yuv文件
				//y_size = inputCodecCtx->width * inputCodecCtx->height;    
				//fwrite(pFrameYUV->data[0],1,y_size,fp_yuv);    //Y   
				//fwrite(pFrameYUV->data[1],1,y_size/4,fp_yuv);  //U  
				//fwrite(pFrameYUV->data[2],1,y_size/4,fp_yuv);  //V  

				
				

				// 写入时间
				pFrameYUV->pts = (clock() - start) / 3;
				printf("frame time is %d \n", pFrameYUV->pts);
				int got_picture2 = 0;
				// 将YUV转成h264
				int rets = avcodec_encode_video2(outputCodecCtx, &outputPct, pFrameYUV, &got_picture2);
					
				if(rets < 0){
					printf("Failed to encode! \n");
					return -1;
				}
				if (got_picture2==1){

					
					printf("packet size %d  ", outputPct.size);

					// pkt相当于一帧的数据， 此处是h264编码
					// int rets = av_write_frame(outputFmtCtx, &outputPct);
					if(NULL != encoderCallback){
						encoderCallback(outputPct.data, outputPct.size);
					}

					av_free_packet(&outputPct);

					// 通过easypush推送到服务器
					return 1;
				}
			}
		}
	}
	return 0;
}