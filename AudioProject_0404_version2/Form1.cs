using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/****AUDIO START*****/
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Numerics;
/****AUDIO END*****/

/****THREAD START*****/
using System.Threading;
/****THREAD END*****/


namespace AudioProject_0404_version2
{
    public partial class Form1 : Form
    {
        //Initial 
        const int RATE = 44100; // Sample rate of the sound card
        private int BUFFERSIZE = (int)Math.Pow(2, 11);
        private Thread Real_graph;
        public BufferedWaveProvider bwp;
        public Int32 envelopeMax;
        bool fft_onoff = false;
        public Form1()
        {
            InitializeComponent();

            //Initial NAudio Wave
            int devcount = WaveIn.DeviceCount;
            WaveIn WI = new WaveIn();
            WI.DeviceNumber = 0;
            WI.WaveFormat = new NAudio.Wave.WaveFormat(RATE,1); //RATE,1
            WI.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);

            //create a wave buffer and start the recording
            WI.DataAvailable += new EventHandler<WaveInEventArgs>(WI_DataAvailable);
            bwp = new BufferedWaveProvider(WI.WaveFormat);

            bwp.DiscardOnBufferOverflow = true;
            WI.StartRecording();
        }

        //adds data to the audio recording buffer
        void WI_DataAvailable(object sender, WaveInEventArgs e)
        {
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        public void updateAudioGraph()
        {
            //read the bytes from the stream
            int frameSize = BUFFERSIZE;
            int SAMPLE_RESOLUTION = 16;
            int BYTES_PER_POINT = SAMPLE_RESOLUTION / 8;

            while (true)
            {
                var frames = new byte[frameSize];
                bwp.Read(frames, 0, frameSize);
                if (frames.Length == 0) 
                {
                    //textBox1.Text = "frames.Length==0 err\n"; 
                    return;
                }
                if (frames[frameSize-2] == 0)
                {
                    //textBox1.Text = "frames[frameSize-2]==0 err\n";
                    return;
                }

                //convert it to int32 ??
                Int32[] vals = new int[frames.Length / BYTES_PER_POINT];
                double[] Xs = new double[frames.Length / BYTES_PER_POINT];
                double[] Ys = new double[frames.Length / BYTES_PER_POINT];
                double[] Xs2 = new double[frames.Length / BYTES_PER_POINT];
                double[] Ys2 = new double[frames.Length / BYTES_PER_POINT];

                for (int i = 0; i < vals.Length; i++)
                {
                    //bit shift the byte buffer into the right variable format
                    byte hByte = frames[i * 2 + 1];
                    byte lByte = frames[i * 2 + 0];
                    vals[i] = (int)(short)((hByte << 8) | lByte); //why?
                    Xs[i] = i;
                    Ys[i] = vals[i];
                    Xs2[i] = (double)i/Ys.Length*RATE/1000.0;

                }
                if (chart1.IsHandleCreated)
                {
                    this.Invoke(
                        (MethodInvoker)delegate { 
                            update_Realtime_Graph(ref Xs, ref Ys); 
                         }
                    );
                }
                else
                {
                    //...
                    //textBox1.Text += "char1.IsHandleCreated else case\n";
                }

                if (fft_onoff == true)
                {
                    Ys2 = FFT(Ys);
                    if (chart2.IsHandleCreated)
                    {
                        this.Invoke(
                            (MethodInvoker)delegate
                            {
                                update_Realtime_fft_Graph(ref Xs2, ref Ys2);
                            }
                        );
                    }
                    else
                    {
                        //....
                    }
                }
                Thread.Sleep(10);
            }
        }

        private void update_Realtime_Graph(ref double[] x, ref double[] y)
        {
            //update the displays
            chart1.Series["Series1"].Points.Clear();
            for(int i = 0; i < x.Length-1; i++)
            {
                chart1.Series["Series1"].Points.AddY(y[i]);
            }
        }

        private void update_Realtime_fft_Graph(ref double[] x, ref double[] y)
        {
            //update the fft displays
            chart2.Series["Series1"].Points.Clear();
            for(int i = 0; i <x.Length/50-1; i++)
            {
                chart2.Series["Series1"].Points.AddY(y[i]);
            }
        }

        //FFT START
        private void button1_Click(object sender, EventArgs e)
        {
            fft_onoff = true;
            Real_graph = new Thread(new ThreadStart(this.updateAudioGraph));
            Real_graph.IsBackground = true;
            Real_graph.Start();
        }

        //END CODE
        private void button2_Click(object sender, EventArgs e)
        {
            fft_onoff = false;
            Real_graph.Abort();
            Real_graph.Join();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        
        //NORMAL DATA
        private void button3_Click(object sender, EventArgs e)
        {
            Real_graph = new Thread(new ThreadStart(this.updateAudioGraph));
            Real_graph.IsBackground = true;
            Real_graph.Start();
        }

        //FFT MATH
        public double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length]; //this is where is where we will store the output(fft)
            Complex[] fftComplex = new Complex[data.Length]; // the FFT function requires complex format

            for (int i = 0; i < data.Length; i++)
            {
                fftComplex[i] = new Complex(data[i], 0.0);
            }

            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);

            for(int i = 0; i < data.Length; i++)
            {
                fft[i] = fftComplex[i].Magnitude; //back to double
                //fft[i] = Math.Log10(fft[i]); //convert to DB
            }
            return fft;
        }
    }
}
