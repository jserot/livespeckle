using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace ImageProcessor
{
    /*
    GENIKA INFO: This DLL is an external image processor for you to add image processing in Genika.
    GENIKA INFO: This DLL is executed when an image is received from the camera grabbing thread and applies to ALL IMAGES that willbe either saved to disk or displayed.
    GENIKA INFO: This DLL isn't static and is instantiated when Genika opens up. So you can use persistant variable members.
    GENIKA INFO: You may add you own windows and classes if needed.
    */

    public class ImageCam
    {
        /* GENIKA INFO: ImageCam is the image container used in Genika.
           GENIKA INFO: This is the image format passed by Genika to the DLL. */
        public ImageCam(int newWidth, int newHeight, Byte[] newBuffer, Byte[] newDisplayBuffer, bool color, Byte[] newRawbuffer, string newDay, string newMonth, string newYear, string newHour, string newMinute, string newSecond, string newMillisecond, string newTicks)
        {
            Width = newWidth;
            Height = newHeight;
            Buffer = newBuffer;
            DisplayBuffer = newDisplayBuffer;
            Color = color;
            Rawbuffer = newRawbuffer;
            Day = newDay; // Time stamping data
            Month = newMonth;
            Year = newYear;
            Hour = newHour;
            Minute = newMinute;
            Second = newSecond;
            Millisecond = newMillisecond;
            Ticks = newTicks;
        }
        public int Width; // The width of the image. 
        public int Height; // The height of the image.
        public Byte[] Buffer; // The raw image data when in 8 bits, or if a 12/14/16 bits image has been converted for display by Genika. 
        public Byte[] DisplayBuffer; //Buffer for image display, convertion is done after the DLL has been called. Should be empty.
        public Byte[] Rawbuffer; // Raw image when in more than 8 bits mode. Pixels are stored on 2 bytes.
        public bool Color; // If false the buffer contains a mono image. If true the image is raw, i.e. not debayerized.
        public string Day; // Time stamping datas
        public string Month;
        public string Year;
        public string Hour;
        public string Minute;
        public string Second;
        public string Millisecond;
        public string Ticks;
        public bool HasBeenStabilized; //Has the image been stabilized by Genika by CoG or other mean.
        public long Number; //Image number in sequence.
    }

   

    public class ImageProcessor
    {
        //DLL required members
        public string ImageFormat;
        /*
        This string is managed by Genika directly.
        It gives the image encoding as follows :
        - Mono8/Mono12/Mono14/Mono16
        - BayerBG8/BayerBG12/BayerBG16
        - BayerGB8/BayerGB12/BayerGB16
        - BayerGR8/BayerGR12/BayerGR16
        - BayerRG8/BayerRG12/BayerRG16
         */

        private int Offset = 100;
        private SettingWindow Settings; //Setting window
        private bool ImageProcessed = false; //Threading flag
        private Thread ProcessingThread; //Processing thread
        private bool ThreadFlag = false; //Processing thread flag
        private AutoResetEvent ThreadEvent; //Processing thread event
        private ImageCam ThreadedProcessingImageCam; //ImageCam objet to pass the image to the thread
        private ThreadWindow threadwin; //Window of the threaded image processing

        public ImageProcessor()
        {
            //Class contructor.
            //Add your class inits here

            Settings = new SettingWindow();
        }

        public string m_GetFilterName()
        {
            /*
             * This methode is called by Genika to get the name of the filter that will be displayed next to the combobox
             */
            return "Process example";
        }

        public void m_ProcessImage(ref ImageCam Image)
        {
            /*
             Main processing method that is called from Genika.
             The image is passed as a reference with the ImageCam Image variable.
             !! be carefull : the image size may change with AoI in Genika, as well as the image format.
             The image can be modified directly from this method.

            In Genika this processing is positioned first and is executed before any other processing.
            Note that image recenter as performed by Genika will be skipped if you flag the ImageCam HasBeenRecentered boolean. So you can do you own image translation.
            This method is performed before :
            - Image recentering
            - FL calculation
            - Hot pixel map correction
            - Image stacking
            - Dark processing
            - Histogram thread triggering
            - Display thread triggering
            - Dark contruction
            - Focusing assistant
            - Autofocusing thread triggering
            */

            if (ImageFormat=="Mono8")
            {
                //*****************In line processing : same thread as Genika's calling thread***********************
                for (int i=0; i<Image.Buffer.Length/10; i++)
                {
                    //Browse the image buffer
                    Image.Buffer[i] = (byte)((Image.Buffer[i] + Settings.Offset) % 255);
                    //Add offset modulo 255 to the first tenth of the image; this is absolutly NOT optimized, divisions are a CPU killer !
                }
                //Image has been modified in line with Genika's call.

                //*****************Off line processing : different thread than Genika's calling thread***********************
                //Theaded processing : that is done without hindering the Genika calling method
                //Is the thread done with the previous image ?
                //This flag is set by the other thread.
                if (ImageProcessed)
                {
                    //Deep copy the imageCam to the threaded ImageCam objet
                    ThreadedProcessingImageCam = ReflectionCloner.DeepFieldClone(Image);
                    //Set event
                    ThreadEvent.Set();
                    //Now the copy is processed by the other thread as best effort.
                }
            }
        }

        public void m_ProcessorHasBeenActivated()
        {
            /*
             This method is called when the processor checkbox is checked from genika.
             This is where you should init your own windows and threads
             */

            //In this example, this shows the setting window
            Settings.Show();
            //Then we start the treaded processing
            //First display the window
            threadwin = new ThreadWindow();
            threadwin.Show();
            //Start up the thread, set the flag, instanciate the event
            ThreadEvent = new AutoResetEvent(false);
            ThreadFlag = true;
            //Set the flag to process the first image
            ImageProcessed = true;
            ProcessingThread = new Thread(ThreadedImageProcessing);
            ProcessingThread.Start();
        }

        public void m_ProcessorHasBeenDeActivated()
        {
            /*
             This method is called when the processor checkbox is unchecked from Genika.
             This is where you should close/hide your own windows and terminate your threads
             */

            //In this example, this hides the setting window
            Settings.Hide();
            //Now we manage the threaded part
            //Stop the thread by setting the flag
            ThreadFlag = false;
            //Wait the thread to stop
            ProcessingThread.Join();
            //Close the window
            threadwin.Close();

        }

        public void m_NewAoIInGenika(int Width, int Heigh)
        {
            /*
            This method is called when a new AoI has been defined in Genika
            That means the payload size has changed !!
            */

            //Display AoI in settings
            Settings.AoI_label.Text = Width.ToString() + " / " + Heigh.ToString();
        }

        public void m_NewImageFormatInGenika(string ImageFormat)
        {
            /*
            This method is called when a new format has been defined in Genika
            That means the payload size has changed !!
            */

            //Display format in settings
            Settings.Format_label.Text = ImageFormat;
        }

        //***************************************************************************************************************************************
        //Example Methods************************************************************************************************************************
        //***************************************************************************************************************************************

        private void ThreadedImageProcessing()
        {
            while (ThreadFlag)
            {
                //Set palette
                Bitmap AlignmentBMP = new Bitmap(128, 128, PixelFormat.Format8bppIndexed);
                ColorPalette colorPalette = AlignmentBMP.Palette;
                for (int i = 0; i < 256; i++)
                {
                    colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                }
                AlignmentBMP.Palette = colorPalette;

                //Wait for a thread event for one second. If we don't have the event, the while loop would saturate a core.
                ThreadEvent.WaitOne(1000);
                if (ImageProcessed && ThreadedProcessingImageCam != null)
                {
                    //Previous image has been processed, we have a new one to process
                    //We set the flag to false so the event could not be raise again by the processor call back
                    ImageProcessed = false;

                    // HERE WE ARE : perform the computations !

                    try
                    {
                        //Extract a 128*128 image from the coordinates in a bitmap
                        BitmapData bmpData = AlignmentBMP.LockBits(new Rectangle(0, 0, 128, 128), ImageLockMode.ReadWrite, AlignmentBMP.PixelFormat);
                        IntPtr ptrBmp = bmpData.Scan0;
                        if (ThreadedProcessingImageCam.Buffer != null)
                        {
                            System.Runtime.InteropServices.Marshal.Copy(m_ExtractBufferArea(ThreadedProcessingImageCam.Buffer, ThreadedProcessingImageCam.Width / 2 - 64,
                                 ThreadedProcessingImageCam.Height / 2 - 64, 128, 128, ThreadedProcessingImageCam.Width,
                                ThreadedProcessingImageCam.Height, ThreadedProcessingImageCam.Width), 0, ptrBmp, 128 * 128);
                        }
                        AlignmentBMP.UnlockBits(bmpData);
                        /*
                        We are using now emgu that is the C# wrapper for OpenCV imaging library.
                        We could also use directly through DLL import or Pinvoke.
                        Genika already has the emgu main DLL in its directory, but be carefull :
                        x86 and x64 DLL are differents !
                        You must add the DLL as references in the project.
                        */

                        /*
                        /* 
                        We are now calculating the real FFT image from the extracted image
                        First we convert the bitmap in an emgu image
                        */
                        Image<Gray, float> image = new Image<Gray, System.Single>(AlignmentBMP);
                        //create an empty complexe image
                        IntPtr complexImage = CvInvoke.cvCreateImage(image.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_32F, 2);
                        CvInvoke.cvSetZero(complexImage); 
                        CvInvoke.cvSetImageCOI(complexImage, 1);
                        CvInvoke.cvCopy(image, complexImage, IntPtr.Zero);
                        CvInvoke.cvSetImageCOI(complexImage, 0);
                        //Make a matrix
                        Matrix<float> dft = new Matrix<float>(image.Rows, image.Cols, 2);
                        //Calculate the FFT to the matrix
                        CvInvoke.cvDFT(complexImage, dft, Emgu.CV.CvEnum.CV_DXT.CV_DXT_FORWARD, 0);

                        //The Real part of the Fourier Transform
                        Matrix<float> outReal = new Matrix<float>(image.Size);
                        //The imaginary part of the Fourier Transform
                        Matrix<float> outIm = new Matrix<float>(image.Size);
                        //Splitting the complex into real/imaginary
                        CvInvoke.cvSplit(dft, outReal, outIm, IntPtr.Zero, IntPtr.Zero);
                        //Normalize to 0-255
                        CvInvoke.cvNormalize(outReal, outReal, 0.0, 255.0, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);

                        Image<Gray, float> fftImage = new Image<Gray, float>(outReal.Size);
                        CvInvoke.cvCopy(outReal, fftImage, IntPtr.Zero);
                        threadwin.pictureBox1.Image = fftImage.Bitmap;
                    }
                    catch (Exception e)
                    {
                        //Swallow
                    }
                    //We are ready to process another image !
                    ImageProcessed = true;
                }
            }
        }

        public byte[] m_ExtractBufferArea(byte[] Source, int X, int Y, int Width, int Height, int CamWidth, int CamHeight, int CamAOI_X) 
        {
            //Extract a subpart of the image 
            byte[] Extract = new byte[Width * Height];
            try
            {
                if (X > 0 && Y > 0 && ((X + Width) < CamWidth) && ((Y + Height) < CamHeight)) //check limits
                {
                    int j = 0; //index for dest
                    for (int i = Y * (int)CamAOI_X + X;
                        i < (Y + Height - 1) * CamAOI_X + X + Width;
                        i += (int)CamAOI_X)
                    {
                        Array.Copy(Source, i, Extract, j, Width);
                        j += Width;
                    }
                }
            }
            catch (Exception e)
            {
                //Swallow
            }
            return Extract;
        }

    }
}
