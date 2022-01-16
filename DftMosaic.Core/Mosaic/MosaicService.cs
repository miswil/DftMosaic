using OpenCvSharp;
using System.Drawing;

namespace DftMosaic.Core.Mosaic
{
    public class MosaicService
    {
        public Mat Mosaic(Mat src)
        {
            using var srcF32 = new Mat();
            src.ConvertTo(srcF32, MatType.CV_32F);
            var channels = srcF32.Split();
            var mosaiceds = new Mat[src.Channels()];
            for (int i = 0; i < channels.Length; i++)
            {
                mosaiceds[i] = this.DftOneChannel(channels[i]);
            }

            var mosaiced = new Mat();
            Cv2.Merge(mosaiceds, mosaiced);
            return mosaiced;
        }

        public Mat Unmosaic(Mat src)
        {
            using var srcF32 = new Mat();
            src.ConvertTo(srcF32, MatType.CV_32F);
            var channels = srcF32.Split();
            var unmosaiceds = new Mat[src.Channels()];
            for (int i = 0; i < channels.Length; i++)
            {
                unmosaiceds[i] = this.IdftOneChannel(
                    channels[i]);
            }

            var unmosaiced = new Mat();
            Cv2.Merge(unmosaiceds, unmosaiced);
            return unmosaiced;
        }

        public Rect DftArea(Mat src, Rect requestRect, bool optimizeSize)
        {
            int width = requestRect.Width;
            int height = requestRect.Height;
            if (optimizeSize)
            {
                width = Cv2.GetOptimalDFTSize(width);
                height = Cv2.GetOptimalDFTSize(height);
            }
            width = Math.Min(width, src.Width - requestRect.X);
            height = Math.Min(height, src.Height - requestRect.Y);
            return new(requestRect.X, requestRect.Y, width, height);
        }

        private Mat DftOneChannel(Mat src)
        {
            if (src.Channels() != 1)
            {
                throw new ArgumentException("Channel of the source matrix must be 1.");
            }

            Mat dft = new Mat();
            Cv2.Dft(src, dft, DftFlags.RealOutput);

            return dft;
        }

        private Mat IdftOneChannel(Mat src)
        {
            if (src.Channels() != 1)
            {
                throw new ArgumentException("Channel of the source matrix must be 1.");
            }

            Mat idft = new Mat();
            Cv2.Idft(src, idft, DftFlags.RealOutput | DftFlags.Scale);

            return idft;
        }

        public MosaicScale Scale(Mat mat)
        {
            mat.MinMaxIdx(out double min, out double max);
            var alpha = 1.0 / (max - min);
            var beta = -min * alpha;
            return new MosaicScale
            {
                Alpha = alpha,
                Beta = beta,
            };
        }
    }
}