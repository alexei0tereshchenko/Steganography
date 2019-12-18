using System;
using System.IO;
using System.IO.Compression;

namespace Steganography
{
    class Program
    {
       static int bytesPerSample;
       static WaveStream sourceStream;
       static Stream destinationStream;

        static void Main(string[] args)
        {
            MemoryStream message = new MemoryStream();
            //Console.WriteLine("Specify key file location: ");
            //String keyFileLocation = Console.ReadLine();

            String keyFileLocation = "C:\\Users\\alts0518\\source\\Steganography\\file";

            //Console.WriteLine("Specify wav file with watermark location: ");
            //String wavFileLocation = Console.ReadLine();
            String wavFileLocation = "C:\\Users\\alts0518\\source\\Steganography\\WithWM.wav";

            FileStream audioFileStream = new FileStream(wavFileLocation, FileMode.Open);
            sourceStream = new WaveStream(audioFileStream);

            Stream keyStream = new FileStream(keyFileLocation, FileMode.Open);

            bytesPerSample = sourceStream.Format.wBitsPerSample / 8;
           
            //Extract(message, keyStream);

            //Console.WriteLine("The hidden message: ");
            //message.Seek(0, SeekOrigin.Begin);
            //string ret = new StreamReader(message).ReadToEnd();
            //Console.WriteLine(ret);

            destinationStream = new FileStream("C:\\Users\\alts0518\\source\\Steganography\\doubleWM.wav", FileMode.Create);

            String messageFileLocation = "C:\\Users\\alts0518\\source\\Steganography\\WM.txt";
            Stream messageStream = new FileStream(messageFileLocation, FileMode.Open);

            //1)
            Hide(messageStream, keyStream);

            
            //2)
            GZipStream sourceM = new GZipStream(audioFileStream, CompressionMode.Compress);
            GZipStream destinationM = new GZipStream(destinationStream, CompressionMode.Compress);
            
            
            //3)
            Double sourceY = CalculateY(sourceM, audioFileStream);
            Double destinationY = CalculateY(destinationM, audioFileStream);

            Double delta = Math.Abs(sourceY - destinationY) * 100;
            Console.WriteLine("Recieved delta: " + delta.ToString() + "%");
            bool isWmFound;
            if (delta < 0.05)
                isWmFound = true;
            else isWmFound = false;
            Console.WriteLine("isWmFound: " + isWmFound);
        }

        private static Double CalculateY (GZipStream afterCompress, Stream beforeCompress)
        {
            Double divident = afterCompress.BaseStream.Length;
            Double divider = beforeCompress.Length;
            return (divident / divider);
        }

        private static void Hide(Stream messageStream, Stream keyStream)
        {

            byte[] waveBuffer = new byte[bytesPerSample];
            byte message, bit, waveByte;
            int messageBuffer; //receives the next byte of the message or -1
            int keyByte; //distance of the next carrier sample

            while ((messageBuffer = messageStream.ReadByte()) >= 0)
            {
                //read one byte of the message stream
                message = (byte)messageBuffer;

                //for each bit in message
                for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                {

                    //read a byte from the key
                    keyByte = GetKeyValue(keyStream);

                    //skip a couple of samples
                    for (int n = 0; n < keyByte - 1; n++)
                    {
                        //copy one sample from the clean stream to the carrier stream
                        sourceStream.Copy(waveBuffer, 0, waveBuffer.Length, destinationStream);
                    }

                    //read one sample from the wave stream
                    sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
                    waveByte = waveBuffer[bytesPerSample - 1];

                    //get the next bit from the current message byte...
                    bit = (byte)(((message & (byte)(1 << bitIndex)) > 0) ? 1 : 0);

                    //...place it in the last bit of the sample
                    if ((bit == 1) && ((waveByte % 2) == 0))
                    {
                        waveByte += 1;
                    }
                    else if ((bit == 0) && ((waveByte % 2) == 1))
                    {
                        waveByte -= 1;
                    }

                    waveBuffer[bytesPerSample - 1] = waveByte;

                    //write the result to destinationStream
                    destinationStream.Write(waveBuffer, 0, bytesPerSample);
                }
            }

            //copy the rest of the wave without changes
            waveBuffer = new byte[sourceStream.Length - sourceStream.Position];
            sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
            destinationStream.Write(waveBuffer, 0, waveBuffer.Length);
        }

        static private void Extract(Stream messageStream, Stream keyStream)
        {

            byte[] waveBuffer = new byte[bytesPerSample];
            byte message, bit, waveByte;
            int messageLength = 0; 
            int keyByte; 

            while ((messageLength == 0 || messageStream.Length < messageLength))
            {
                
                message = 0;

                for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                {
                    keyByte = GetKeyValue(keyStream);

                    for (int n = 0; n < keyByte - 1; n++)
                    {
                        sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
                    }
                    sourceStream.Read(waveBuffer, 0, waveBuffer.Length);
                    waveByte = waveBuffer[bytesPerSample - 1];

                    bit = (byte)(((waveByte % 2) == 0) ? 0 : 1);

                    message += (byte)(bit << bitIndex);
                }

                messageStream.WriteByte(message);

                if (messageLength == 0 && messageStream.Length == 4)
                {
                    messageStream.Seek(0, SeekOrigin.Begin);
                    messageLength = new BinaryReader(messageStream).ReadInt32();
                    messageStream.Seek(0, SeekOrigin.Begin);
                    messageStream.SetLength(0);
                }
            }
        }
        private static byte GetKeyValue(Stream keyStream)
        {
            int keyValue;
            if ((keyValue = keyStream.ReadByte()) < 0)
            {
                keyStream.Seek(0, SeekOrigin.Begin);
                keyValue = keyStream.ReadByte();
                if (keyValue == 0) { keyValue = 1; }
            }
            return (byte)keyValue;
        }
    }
}
