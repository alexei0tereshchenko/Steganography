using System;
using System.IO;

namespace Steganography
{
    class Program
    {
       static int bytesPerSample;
       static WaveStream sourceStream;

        static void Main(string[] args)
        {
            MemoryStream message = new MemoryStream();
            //Console.WriteLine("Specify key file location: ");
            //String keyFileLocation = Console.ReadLine();

            String keyFileLocation = "C:\\Users\\alts0518\\Desktop\\file";

            //Console.WriteLine("Specify wav file with watermark location: ");
            //String wavFileLocation = Console.ReadLine();
            String wavFileLocation = "C:\\Users\\alts0518\\Desktop\\aaa.wav";

            FileStream audioFileStream = new FileStream(wavFileLocation, FileMode.Open);
            sourceStream = new WaveStream(audioFileStream);

            Stream keyStream = new FileStream(keyFileLocation, FileMode.Open);

            bytesPerSample = sourceStream.Format.wBitsPerSample / 8;

            Extract(message, keyStream);

            Console.WriteLine("The hidden message: ");
            message.Seek(0, SeekOrigin.Begin);
            string ret = new StreamReader(message).ReadToEnd();
            Console.WriteLine(ret);
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
