








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
AVPacket		*packet = NULL;	// ���ڴ��֡����
SwsContext		*rgb_to_yuv = NULL;
EncoderCallback encoderCallback = NULL;
clock_t			start, finish, last_finish;


AVPacket		outputPct;
bool			hasnot_init_packet = true;
AVCodecContext	*outputCodecCtx = NULL;
AVCodec			*outputCodec = NULL;
bool			first_frame = true;			// ��һ֡Ϊsps pps��ϵ�֡, Ӧ����ȡ����

int encoder_init(){
	av_register_all();
	avformat_network_init();
	inputFmtCtx = avformat_alloc_context();
	avdevice_register_all();


	// ʹ��gdigrab��ȡ����
	AVDictionary* options = NULL;

	// �򿪲����gdigrab
	inputFormat = av_find_input_format("gdigrab");
	if(avformat_open_input(&inputFmtCtx, "desktop" , inputFormat, &options)!=0){
		printf("Couldn't open input stream.\n");
		return -1;
	}


	gdigrab* pGdi = (gdigrab*)inputFmtCtx->priv_data;
	pGdi->draw_mouse = 0;
	
	// av_dump_format(inputFmtCtx, 0, output_file, 1);

	// ���
	if(avformat_find_stream_info(inputFmtCtx,NULL)<0) {
		printf("Couldn't find stream information.\n");
		return -1;
	}

	// ��ȡ��Ƶͨ����
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

	// ��ȡ��Ƶ���ı����ʽ
	inputCodecCtx = inputFmtCtx->streams[videoindex]->codec;
	inputCodec = avcodec_find_decoder(inputCodecCtx->codec_id);
	if(inputCodec==NULL)
	{
		printf("Codec not found.\n");
		return -1;
	}

	// �ú������ڳ�ʼ��һ������Ƶ���������AVCodecContext
	// ���������ʱ����
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

	


	// ΪGBRAתYUV��ʽ��׼��
	rgb_to_yuv = sws_getContext(
		inputCodecCtx->width, 
		inputCodecCtx->height, 
		inputCodecCtx->pix_fmt, 
		inputCodecCtx->width, 
		inputCodecCtx->height, 
		AV_PIX_FMT_YUV420P, 
		SWS_BICUBIC, 
		NULL, NULL, NULL);



	///  h264 �����ʼ�� start

	// ���ñ�Ҫ�Ĳ���
	outputCodec = avcodec_find_encoder(AV_CODEC_ID_H264);

	outputCodecCtx = avcodec_alloc_context3(outputCodec);
	outputCodecCtx->codec_id = AV_CODEC_ID_H264;
	outputCodecCtx->codec_type = AVMEDIA_TYPE_VIDEO;
	outputCodecCtx->pix_fmt = AV_PIX_FMT_YUV420P;
	outputCodecCtx->width = inputCodecCtx->width;  
	outputCodecCtx->height = inputCodecCtx->height;
	outputCodecCtx->bit_rate = 4000000;  
	outputCodecCtx->gop_size = 10;		// ����֡���һ��I֡

	outputCodecCtx->time_base.num = 1;  
	outputCodecCtx->time_base.den = 25;  

	//H264
	outputCodecCtx->me_range = 16;
	outputCodecCtx->max_qdiff = 4;
	outputCodecCtx->qcompress = 0.6;

	outputCodecCtx->qmin = 10;
	outputCodecCtx->qmax = 51;

	//��ѡ����
	outputCodecCtx->max_b_frames=3;

	av_opt_set(outputCodecCtx->priv_data, "preset", "superfast", 0);  
  
    // ʵʱ����ؼ�����䣬������������ν  
    av_opt_set(outputCodecCtx->priv_data, "tune", "zerolatency", 0);

	// �򿪱�����
	if(avcodec_open2(outputCodecCtx, outputCodec, NULL) < 0){
		return -1;
	}

	///  h264 �����ʼ�� end



	start = clock();
	last_finish = clock();
	

	return 1;
}

int encoder_close(){

	// ��������е�ͼƬ
	int got_picture2 = 1;
	while(got_picture2 == 1){
		int rets = avcodec_encode_video2(outputCodecCtx, &outputPct, NULL, &got_picture2);
					
		if(rets < 0){
			printf("Failed to encode! \n");
			break; -1;
		}
		if (got_picture2 == 1){

			// pkt�൱��һ֡�����ݣ� �˴���h264����
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


// ���Բ���һ�������h264
int encoder_tryToMakeFrame(){
	if(av_read_frame(inputFmtCtx, packet)>=0) {

		// �����Ƶͨ���Ƿ���ͬ
		if(packet->stream_index == videoindex){
			// ����һ֡��Ƶ���ݡ�����һ��ѹ������Ľṹ��AVPacket�����һ�������Ľṹ��AVFrame
			int ret2 = avcodec_decode_video2(inputCodecCtx, pFrame, &got_picture, packet);		
			if(ret2 < 0){
				printf("Decode Error.\n");
				return -1;
			}

			if(got_picture){

				if(hasnot_init_packet){
					hasnot_init_packet = false;

					// ������Ļ��С����AVPacket
					int picture_size = avpicture_get_size(AV_PIX_FMT_YUV420P, pFrame->width, pFrame->height);
					av_new_packet(&outputPct, picture_size);

					// ������Ļ��С����AVFrame
					uint8_t * picture_buf = (uint8_t *)av_malloc(picture_size);
					avpicture_fill((AVPicture *)pFrameYUV, picture_buf, outputCodecCtx->pix_fmt, pFrame->width, pFrame->height);
				}


				// ת��YUV
				pFrameYUV->width = pFrame->width;
				pFrameYUV->height = pFrame->height;
				pFrameYUV->format = AV_PIX_FMT_YUV420P;
				//�������߂������� 
				//��һ������������ sws_getContext ��ȡ�õą����� 
				//�ڶ��� src �������� dst �քeָ��input �� output �� buffer�� 
				//������ srcStride �����߂� dstStride �քeָ�� input �� output �� stride�������֪��ʲ�N�� stride�����ҿ����Ȱ���������ÿһ�е� byte ���� 
				//���Ă� srcSliceY�����]�����˼������ָ��һ��Ҫ̎���λ�ã��@�e���Ǐ��^̎������ֱ����0����֪����Ԕ���f�����ˣ����ԅ��� swscale.h ���]�⡣ 
				//���傀srcSliceHָ���� source slice �ĸ߶ȡ�
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

				// д��yuv�ļ�
				//y_size = inputCodecCtx->width * inputCodecCtx->height;    
				//fwrite(pFrameYUV->data[0],1,y_size,fp_yuv);    //Y   
				//fwrite(pFrameYUV->data[1],1,y_size/4,fp_yuv);  //U  
				//fwrite(pFrameYUV->data[2],1,y_size/4,fp_yuv);  //V  

				
				

				// д��ʱ��
				pFrameYUV->pts = (clock() - start) / 3;
				printf("frame time is %d \n", pFrameYUV->pts);
				int got_picture2 = 0;
				// ��YUVת��h264
				int rets = avcodec_encode_video2(outputCodecCtx, &outputPct, pFrameYUV, &got_picture2);
					
				if(rets < 0){
					printf("Failed to encode! \n");
					return -1;
				}
				if (got_picture2==1){

					
					printf("packet size %d  ", outputPct.size);

					// pkt�൱��һ֡�����ݣ� �˴���h264����
					// int rets = av_write_frame(outputFmtCtx, &outputPct);
					if(NULL != encoderCallback){
						encoderCallback(outputPct.data, outputPct.size);
					}

					av_free_packet(&outputPct);

					// ͨ��easypush���͵�������
					return 1;
				}
			}
		}
	}
	return 0;
}