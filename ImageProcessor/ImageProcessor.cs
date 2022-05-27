// Genika DLL-based Image Processor for real-time computing of auto-correlation
// Author : J. Serot (jocelyn.serot@free.fr)
// Version : 0.1

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
    This DLL is an external image processor for you to add image processing in Genika.
    The corresponding DLL will be executed whenever an image is received from the camera grabbing thread.
    It therefore applies to ALL IMAGES that will be either saved to disk or displayed.
    This DLL isn't static and is instantiated when Genika opens up. So you can use persistant variable members.
    */

    [Flags]
    public enum VisuOptions
    {
        None = 0,
        LogScale = 1,
        ShiftQuadrants = 2,
        Resize = 4
    }

    public class ImageCam
    {
        /*
        ImageCam is the image container class used in Genika.
        Do not modify.
        */
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

        // Genika-related members
        private bool ImageProcessed = false; //Threading flag
        private Thread ProcessingThread; //Processing thread
        private bool ThreadFlag = false; //Processing thread flag
        private AutoResetEvent ThreadEvent; //Processing thread event
        private ImageCam ThreadedProcessingImageCam; //ImageCam objet to pass the image to the thread
        private ThreadWindow threadwin; //Window of the threaded image processing

        // Application-specific members

        int wSize = 128;    // Size of the processing window
        Matrix<float> sps;  // Power spectrum accumulator
        uint accCount;      // Acumulator counter

        public ImageProcessor()
        {
            // Class-specific initialisation here
            // Settings = new SettingWindow(); // No Setting windows here !
        }

        public string m_GetFilterName()
        {
            /*
             * This methode is called by Genika to get the name of the filter that will be displayed next to the combobox
             */
            return "LiveSpeckle 1.0";
        }

        public void m_ProcessImage(ref ImageCam Image)
        {
            /*
             Main processing method called from Genika.
             The image is passed as a reference with the ImageCam Image variable.
             !! Warning : the image size may change with AoI in Genika, as well as the image format.
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
            *************************************************************************
            - Image saving => your image processing applies to display AND image save 
            *************************************************************************
            */

            if (ImageFormat=="Mono8")
            {
                //*****************In line processing : same thread as Genika's calling thread***********************
                // NOTHING HERE

                //*****************Off line processing : different thread than Genika's calling thread***********************
                
                //Theaded processing : performed without hindering the Genika calling method                
                if (ImageProcessed)  // Has the concurrent thread finished processing with the previous image ?
                {
                    //Deep copy the imageCam to the threaded ImageCam objet
                    ThreadedProcessingImageCam = ReflectionCloner.DeepFieldClone(Image);
                    //Notify
                    ThreadEvent.Set();
                }
            }
        }

        public void m_ProcessorHasBeenActivated()
        {
            /*
             This method is called when the processor checkbox is checked from genika.
             This is where you should init your own windows and threads
             */

            // Settings.Show();  // No settings window here
            //First display the window
            threadwin = new ThreadWindow();
            threadwin.Show();
            //Start up the thread, set the flag, instanciate the event
            ThreadEvent = new AutoResetEvent(false);
            ThreadFlag = true;
            //Set the flag to process the first image
            ImageProcessed = true;
            // Initialize application-specific variables
            wSize = 128;
            sps = new Matrix<float>(wSize, wSize);  // Power spectrum accumulator
            sps.SetZero();
            accCount = 0;
            // Let's go !
            ProcessingThread = new Thread(ThreadedImageProcessing);
            ProcessingThread.Start();
        }

        public void m_ProcessorHasBeenDeActivated()
        {
            /*
             This method is called when the processor checkbox is unchecked from Genika.
             This is where you should close/hide your own windows and terminate your threads
             */

            // Settings.Hide(); // No settings window here
            //Stop the thread 
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
            */
        }

        public void m_NewImageFormatInGenika(string ImageFormat)
        {
            /*
            This method is called when a new format has been defined in Genika
            */
       }

        private void ThreadedImageProcessing()
        {
            while (ThreadFlag)
            {
                uint accLength = (uint)threadwin.numericUpDown1.Value;
                int filterSize = (int)threadwin.numericUpDown2.Value;
                VisuOptions opts = threadwin.checkBox1.Checked ? VisuOptions.Resize : VisuOptions.None;

                //Wait for a thread event for one second. If we don't have the event, the while loop would saturate a core.
                ThreadEvent.WaitOne(1000);
                if (ImageProcessed && ThreadedProcessingImageCam != null)
                {
                    //Previous image has been processed, we have a new one to process
                    //Set the flag to false so the event could not be raised again by the processor call back
                    ImageProcessed = false;
                    //Let's work now
                    try
                    {
                        // Get Image from Genika Cam
                        Image<Gray, float> img = get_image();

                        // Compute power spectrum
                        Matrix<float> ps = power_spectrum(img);

                        // Accumulate
                        sps = sps.Add(ps);
                        accCount = accCount + 1;

                        // Build and display results

                        Image<Gray, float> visu2 = visu(ps, VisuOptions.ShiftQuadrants | VisuOptions.LogScale);
                        if (opts.HasFlag(VisuOptions.Resize))
                        {
                            img = img.Resize(2.0, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
                            visu2 = visu2.Resize(2.0, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
                        }
                        threadwin.pictureBox1.Image = img.ToBitmap();
                        threadwin.pictureBox2.Image = visu2.ToBitmap();

                        if (accCount >= accLength)  // Each n-th frame
                        {
                            // Compute auto-correlation of accumulated power spectrum
                            Matrix<float> ac = ifft(sps);
                            // Filter auto-correlation
                            Matrix<float> fac;
                            shift_quadrants(ref ac); // Must shift quadrants BEFORE filtering to avoid side effects
                            if (filterSize > 1)
                            {
                                fac = new Matrix<float>(ac.Rows, ac.Cols, 1);
                                CvInvoke.cvSmooth(ac, fac, Emgu.CV.CvEnum.SMOOTH_TYPE.CV_BLUR, filterSize, filterSize, 0, 0);
                                CvInvoke.cvSub(ac, fac, fac, IntPtr.Zero);
                            }
                            else
                                fac = ac;
                            // Display results
                            Image<Gray, float> visu3 = visu(sps, VisuOptions.ShiftQuadrants | VisuOptions.LogScale);
                            Image<Rgb, byte> visu4 = color_visu(fac);
                            if (opts.HasFlag(VisuOptions.Resize))
                            {
                                visu3 = visu3.Resize(2.0, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
                                visu4 = visu4.Resize(2.0, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
                            }
                            threadwin.pictureBox3.Image = visu3.ToBitmap();
                            threadwin.pictureBox4.Image = visu4.ToBitmap();
                            // Reset accumulation
                            sps.SetZero();
                            accCount = 0;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                    }
                    // Ready to process another image !
                    ImageProcessed = true;
                }
            }
        }

        private Image<Gray,float> get_image()
        {
            // TODO : find a more direct way to build an EMGU Image from a buffer..
            Bitmap bmp = new Bitmap(wSize, wSize, PixelFormat.Format8bppIndexed);
            ColorPalette colorPalette = bmp.Palette;
            for (int i = 0; i < 256; i++) colorPalette.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = colorPalette;
            Byte[] buf = m_ExtractBufferArea(ThreadedProcessingImageCam.Buffer,
                                                    ThreadedProcessingImageCam.Width / 2 - wSize / 2,
                                                    ThreadedProcessingImageCam.Height / 2 - wSize / 2,
                                                    wSize,
                                                    wSize,
                                                    ThreadedProcessingImageCam.Width,
                                                    ThreadedProcessingImageCam.Height,
                                                    ThreadedProcessingImageCam.Width);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, wSize, wSize), ImageLockMode.ReadWrite, bmp.PixelFormat);
            if (ThreadedProcessingImageCam.Buffer != null)
                System.Runtime.InteropServices.Marshal.Copy(buf, 0, bmpData.Scan0, buf.Length);
            bmp.UnlockBits(bmpData);
            return new Image<Gray, System.Single>(bmp);
        }

        public byte[] m_ExtractBufferArea(byte[] Source, int X, int Y, int Width, int Height, int CamWidth, int CamHeight, int CamAOI_X) 
        {
            //Extract subpart of image buffer (8 bits only)
            byte[] Extract = new byte[Width * Height];
            try
            {
                if (X > 0 && Y > 0 && ((X + Width) < CamWidth) && ((Y + Height) < CamHeight)) 
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
                MessageBox.Show(e.Message);
            }
            return Extract;
        }

        Matrix<float> power_spectrum(Image<Gray, float> im)
        {
            IntPtr cim = CvInvoke.cvCreateImage(im.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_32F, 2);
            CvInvoke.cvSetZero(cim);
            CvInvoke.cvSetImageCOI(cim, 1); // Select first channel (0 means all..)
            CvInvoke.cvCopy(im, cim, IntPtr.Zero);
            CvInvoke.cvSetImageCOI(cim, 0);
            // Compute FFT
            Matrix<float> tf = new Matrix<float>(im.Rows, im.Cols, 2);
            CvInvoke.cvDFT(cim, tf, Emgu.CV.CvEnum.CV_DXT.CV_DXT_FORWARD, 0);
            //// Get real and imaginary parts 
            Matrix<float> r = new Matrix<float>(tf.Size);
            Matrix<float> i = new Matrix<float>(tf.Size);
            CvInvoke.cvSplit(tf, r, i, IntPtr.Zero, IntPtr.Zero);
            CvInvoke.cvMul(r, r, r, 1.0);
            CvInvoke.cvMul(i, i, i, 1.0);
            CvInvoke.cvAdd(r, i, r, IntPtr.Zero);
            CvInvoke.cvReleaseImage(ref cim);  // Do not forget ! 
            return r;
        }

        Matrix<float> ifft(Matrix<float> im) // im is a 1-channel image
        {
            IntPtr cim = CvInvoke.cvCreateImage(im.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_32F, 2);
            CvInvoke.cvSetZero(cim);
            CvInvoke.cvSetImageCOI(cim, 1); // Select first channel (0 means all)
            CvInvoke.cvCopy(im, cim, IntPtr.Zero);
            CvInvoke.cvSetImageCOI(cim, 0);
            Matrix<float> tf = new Matrix<float>(im.Rows, im.Cols, 2);
            CvInvoke.cvDFT(cim, tf, Emgu.CV.CvEnum.CV_DXT.CV_DXT_INVERSE, 0);
            // get real and imaginary parts 
            Matrix<float> r = new Matrix<float>(tf.Size);
            Matrix<float> i = new Matrix<float>(tf.Size);
            CvInvoke.cvSplit(tf, r, i, IntPtr.Zero, IntPtr.Zero);
            CvInvoke.cvReleaseImage(ref cim);  // Do not forget ! 
            return r;
        }

        private Image<Gray, float> visu(Matrix<float> src, VisuOptions options = VisuOptions.None)
        {
            Image<Gray, float> res = new Image<Gray, float>(src.Rows, src.Cols);
            CvInvoke.cvConvert(src, res);
            if (options.HasFlag(VisuOptions.ShiftQuadrants)) shift_quadrants(ref res);
            if (options.HasFlag(VisuOptions.LogScale)) CvInvoke.cvLog(res, res);
            CvInvoke.cvNormalize(res, res, 0.0, 255.0, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);
            if (options.HasFlag(VisuOptions.Resize)) res = res.Resize(2.0, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
            return res;
        }

        private Image<Rgb, byte> color_visu(Matrix<float> img, VisuOptions options = VisuOptions.None)
        {
            Image<Gray, float> im = new Image<Gray, float>(img.Rows, img.Cols);
            CvInvoke.cvConvert(img, im);
            if (options.HasFlag(VisuOptions.ShiftQuadrants)) shift_quadrants(ref im);
            CvInvoke.cvNormalize(im, im, 0.0, 1.0, Emgu.CV.CvEnum.NORM_TYPE.CV_MINMAX, IntPtr.Zero);
            Image<Rgb, byte> res = new Image<Rgb, byte>(im.Rows, im.Cols);
            Rgb c;
            float[,,] src = im.Data;
            Byte[,,] dst = res.Data;
            for (int j = 0; j < res.Rows; j++)
                for (int i = 0; i < im.Cols; i++)
                {
                    c = falsecolor(src[i, j, 0], 0.0, 1.0);
                    dst[i, j, 0] = (Byte)(255 * c.Red);
                    dst[i, j, 1] = (Byte)(255 * c.Green);
                    dst[i, j, 2] = (Byte)(255 * c.Blue);
                }
            return res;
        }

        private Rgb falsecolor(double v, double vmin, double vmax)
        {
            Rgb c = new Rgb(1.0, 1.0, 1.0);
            double dv;
            if (v < vmin) v = vmin;
            if (v > vmax) v = vmax;
            dv = vmax - vmin;
            if (v < (vmin + 0.25 * dv))
            {
                c.Red = 0;
                c.Green = 4 * (v - vmin) / dv;
            }
            else if (v < (vmin + 0.5 * dv))
            {
                c.Red = 0;
                c.Blue = 1 + 4 * (vmin + 0.25 * dv - v) / dv;
            }
            else if (v < (vmin + 0.75 * dv))
            {
                c.Red = 4 * (v - vmin - 0.5 * dv) / dv;
                c.Blue = 0;
            }
            else
            {
                c.Green = 1 + 4 * (vmin + 0.75 * dv - v) / dv;
                c.Blue = 0;
            }
            return c;
        }

        private void shift_quadrants(ref Image<Gray, float> m)
        {
            int cx = m.Cols / 2;
            int cy = m.Rows / 2;
            Image<Gray, float> q0 = m.GetSubRect(new Rectangle(0, 0, cx, cy));
            Image<Gray, float> q1 = m.GetSubRect(new Rectangle(cx, 0, cx, cy));
            Image<Gray, float> q2 = m.GetSubRect(new Rectangle(0, cy, cx, cy));
            Image<Gray, float> q3 = m.GetSubRect(new Rectangle(cx, cy, cx, cy));
            Image<Gray, float> tmp = new Image<Gray, float>(cx, cy);
            CvInvoke.cvCopy(q0, tmp, IntPtr.Zero);
            CvInvoke.cvCopy(q3, q0, IntPtr.Zero);
            CvInvoke.cvCopy(tmp, q3, IntPtr.Zero);
            CvInvoke.cvCopy(q1, tmp, IntPtr.Zero);
            CvInvoke.cvCopy(q2, q1, IntPtr.Zero);
            CvInvoke.cvCopy(tmp, q2, IntPtr.Zero);
        }

        private void shift_quadrants(ref Matrix<float> m)
        {
            int cx = m.Cols / 2;
            int cy = m.Rows / 2;
            Matrix<float> q0 = m.GetSubRect(new Rectangle(0, 0, cx, cy));
            Matrix<float> q1 = m.GetSubRect(new Rectangle(cx, 0, cx, cy));
            Matrix<float> q2 = m.GetSubRect(new Rectangle(0, cy, cx, cy));
            Matrix<float> q3 = m.GetSubRect(new Rectangle(cx, cy, cx, cy));
            Matrix<float> tmp = new Matrix<float>(cx, cy);
            CvInvoke.cvCopy(q0, tmp, IntPtr.Zero);
            CvInvoke.cvCopy(q3, q0, IntPtr.Zero);
            CvInvoke.cvCopy(tmp, q3, IntPtr.Zero);
            CvInvoke.cvCopy(q1, tmp, IntPtr.Zero);
            CvInvoke.cvCopy(q2, q1, IntPtr.Zero);
            CvInvoke.cvCopy(tmp, q2, IntPtr.Zero);
        }

    }
}
