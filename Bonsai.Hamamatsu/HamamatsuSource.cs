using Bonsai;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Imaging;
using Hamamatsu.subacq4;
using Hamamatsu.DCAM4;


namespace Bonsai.Hamamatsu
{
    [Description("Generates a sequence of images acquired from the specified Ximea camera.")]
    [Combinator(MethodName = nameof(Generate))]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class HamamatsuSource: Source<IplImage>
    {
        readonly object captureLock = new object();
        IObservable<IplImage> source;
        IntPtr camera;
        MyDcam myCam = new MyDcam();
        //Bitmap frame;
        //IplImage image = new IplImage(frame.Width, frame.Height, frame.depth, 1);
        IplImage output;
        //Array array;
        //OpenCV.Net.Arr arr;
        public int width, height;
        int gain;
        float framerate;
        int exposure;
        int whiteBalanceRed;
        int whiteBalanceGreen;
        int whiteBalanceBlue;
        bool autoGain;
        bool autoExposure;
        int autoWhiteBalance;
        DCAMBUF_FRAME frame = new DCAMBUF_FRAME();
        MyDcamProp exp;
        MyDcamProp trigger;
        bool deviceOpen = false;

        public HamamatsuSource()
        {
            
            Exposure = 1226;

            OffsetX = 432;
            OffsetY = 40;

            ROIWidth = 1000;
            ROIHeight = 988;



            source = Observable.Create<IplImage>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        Load();
                        try
                        {
                            int iframe = 0;
                            while (!cancellationToken.IsCancellationRequested)
                            {

                                
                                frame.iFrame = iframe; //this needs to iterate but need the

                                
                                myCam.buf_copyframe(ref frame);


                                // Lock the bitmap's bits. 
                                //Rectangle rc = new Rectangle(0, 0, frame.width, frame.height);
                                //m_bitmap = new Bitmap(frame.width, frame.height);
                                //SUBACQERR err = subacq.copydib(ref m_bitmap, frame, ref rc, m_lut.inmax, m_lut.inmin);
                                
                                //System.Drawing.Imaging.BitmapData imgData = frame.LockBits
                                //(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, frame.PixelFormat);

                                //IntPtr ptr = imgData.Scan0;



                                OpenCV.Net.Size outSize = new OpenCV.Net.Size(frame.width, frame.height);
                                output = new IplImage(outSize, OpenCV.Net.IplDepth.U8, 1, frame.buf);
                                
                                observer.OnNext(output.Clone());
                                iframe += 1;
                                //frame.UnlockBits(imgData);
                                


                            }
                        }
                        finally { Unload(); }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            })
            .PublishReconnectable()
            .RefCount();
        }
        



        [Description("The ROI width at which to acquire image frames.")]
        public int ROIWidth { get; set; }

        [Description("The frame height at which to acquire image frames.")]
        public int ROIHeight { get; set; }

        [Description("The ROI offset in X axis at which to acquire image frames.")]
        public int OffsetX { get; set; }

        [Description("The ROI offset in Y axis at which to acquire image frames.")]
        public int OffsetY { get; set; }

        //[Range(0, 79)]
        //[Description("The fixed gain value, used when auto gain is disabled.")]
        //[Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        //public int Gain
        //{
        //    get { return gain; }
        //    set
        //    {
        //        gain = value;
        //        if (deviceOpen)
        //        {
        //            myCam.SetParam(PRM.GAIN, value);
        //            
        //        }
        //        
        //    }
        //}

        [Range(0, 5110)]
        [Description("The fixed exposure value, used when auto exposure is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int Exposure
        {
            get { return exposure; }
            set
            {
                exposure = value;
              if (deviceOpen)
              {
                    
                    exp.setvalue((double)exposure);
                    //myCam.SetParam(PRM.EXPOSURE, (Int32)value);
                }
                
                
            }
        }

        

        private void Load()
        {
            // initialise api
            if (!MyDcamApi.init())
            {
                Thread.Sleep(3000);
            }

            //open camera
            if (!myCam.dev_open(0)) 
            {
                deviceOpen = false;
            }

            else
            {
                deviceOpen = true;
            }

            //allocate buffer
            myCam.buf_alloc(6000); //number of frames

            //set capture mode as continuous
            myCam.m_capmode = DCAMCAP_START.SEQUENCE;

            // start capture

            if (!myCam.cap_start()) 
            {
                myCam.buf_release();
            }


            exp = new MyDcamProp(myCam, DCAMIDPROP.EXPOSURETIME);
            trigger = new MyDcamProp(myCam, DCAMIDPROP.TRIGGERSOURCE);


            //set exposure
            exp.setvalue(Exposure);
            //myCam.SetParam(PRM.EXPOSURE, Exposure);

            // Set device gain 
            //myCam.SetParam(PRM.GAIN, Gain);

            // Set image output format to monochrome 8 bit
            //myCam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.MONO8);

            //Set Acquisition Mode
            //myCam.SetParam(PRM.ACQ_TIMING_MODE, 1);

            //Set external trigger source
            trigger.setvalue(DCAMPROP.TRIGGERSOURCE.EXTERNAL);
            //myCam.SetParam(PRM.FRAMERATE, FrameRate);
            
            //Specify ROI params
            //myCam.SetParam(PRM.WIDTH, ROIWidth);
            //myCam.SetParam(PRM.HEIGHT, ROIHeight);
            //myCam.SetParam(PRM.OFFSET_X, OffsetX);
            //myCam.SetParam(PRM.OFFSET_Y, OffsetY);
            //Start acquisition
            //myCam.StartAcquisition();

            

            //Get width and height
            //myCam.GetParam(PRM.HEIGHT, out height);
            //myCam.GetParam(PRM.WIDTH, out width);
            
            
        }

        private void Unload()
        {
            

            //release buffer
            myCam.buf_release();
            //close camera
            myCam.dev_close();
            // uninit api
            MyDcamApi.uninit();


            
        }

        public override IObservable<IplImage> Generate()
        {
            return source;
        }

        

    }
}
