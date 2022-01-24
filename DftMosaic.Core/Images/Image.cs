using OpenCvSharp;

namespace DftMosaic.Core.Images
{
    public class Image
    {
        public Mat Data { get; }
        public MosaicInfo? MosaicInfo { get; }

        public bool IsMosaiced => this.MosaicInfo is not null;

        public Image(Mat data)
        {
            this.Data = data;
        }

        public Image(Mat data, MosaicInfo mosaicInfo) : this(data)
        {
            this.MosaicInfo = mosaicInfo;
        }

        public Image Mosaic(Rect mosaicRequestArea, MosaicType mosaicType, bool optimizeSize = true)
        {
            if (this.MosaicInfo is not null && this.MosaicInfo.Type != mosaicType)
            {
                throw new InvalidOperationException("The specified mosaic type must be same as image mosaic type.");
            }
            return mosaicType switch
            {
                MosaicType.GrayScale => this.MosaicGrayScale(mosaicRequestArea, optimizeSize),
                MosaicType.FullColor => this.MosaicFullColor(mosaicRequestArea, optimizeSize),
                MosaicType.Color => this.MosaicColor(mosaicRequestArea, optimizeSize),
                MosaicType.ShortColor => this.MosaicShortColor(mosaicRequestArea, optimizeSize),
                _ => throw new ArgumentException("invalid mosaic type."),
            };
        }

        private Image MosaicGrayScale(Rect mosaicRequestArea, bool optimizeSize)
        {
            using var grayedOriginal = this.Data.CvtColor(ColorConversionCodes.BGRA2GRAY);
            using var grayedOriginal32F = new Mat();
            grayedOriginal.ConvertTo(grayedOriginal32F, MatType.CV_32F);
            var mosaicedImage = grayedOriginal.CvtColor(ColorConversionCodes.GRAY2BGRA);

            var mosaicArea = mosaicRequestArea;
            if (optimizeSize)
            {
                mosaicArea = this.DftArea(this.Data, mosaicRequestArea, optimizeSize);
            }

            using var mosaiced = this.MosaicImageData(grayedOriginal32F[mosaicArea]);
            using var saveMosaiced = new Mat(mosaiced.Rows, mosaiced.Cols, mosaicedImage.Type(), mosaiced.Data);
            mosaicedImage[mosaicArea] = saveMosaiced;

            return new(mosaicedImage, new(mosaicArea, MosaicType.GrayScale, null));
        }

        private Image MosaicFull64Color(Rect mosaicRequestArea, bool optimizeSize)
        {
            var original64F = new Mat();
            var alpha = this.Data.Depth() switch
            {
                0 or 1 => 1.0 / 255,
                2 or 3 => 1.0 / 65535,
                _ => 1.0,
            };
            this.Data.ConvertTo(original64F, MatType.CV_64F, alpha);
            var mosaicArea = mosaicRequestArea;
            if (optimizeSize)
            {
                mosaicArea = this.DftArea(this.Data, mosaicRequestArea, optimizeSize);
            }

            using var mosaiced = this.MosaicImageData(original64F[mosaicArea]);
            var scale = this.ScaleImage(mosaiced);
            original64F[mosaicArea] = mosaiced * scale.Alpha + Scalar.All(scale.Beta);
            return new(original64F, new(mosaicArea, MosaicType.Full64Color, scale));
        }

        private Image MosaicFullColor(Rect mosaicRequestArea, bool optimizeSize)
        {
            var fullColor = this.MosaicFull64Color(mosaicRequestArea, optimizeSize);
            var mosaiced32F = new Mat();
            fullColor.Data.ConvertTo(mosaiced32F, MatType.CV_32F);
            return new(mosaiced32F, fullColor.MosaicInfo with { Type = MosaicType.FullColor });
        }

        private Image MosaicColor(Rect mosaicRequestArea, bool optimizeSize)
        {
            var fullColor = this.MosaicFull64Color(mosaicRequestArea, optimizeSize);
            var mosaiced16U = new Mat();
            fullColor.Data.ConvertTo(mosaiced16U, MatType.CV_16U, 65535);
            return new(mosaiced16U, fullColor.MosaicInfo with { Type = MosaicType.Color });
        }

        private Image MosaicShortColor(Rect mosaicRequestArea, bool optimizeSize)
        {
            var fullColor = this.MosaicFull64Color(mosaicRequestArea, optimizeSize);
            var mosaiced8U = new Mat();
            fullColor.Data.ConvertTo(mosaiced8U, MatType.CV_8U, 255);
            return new(mosaiced8U, fullColor.MosaicInfo with { Type = MosaicType.ShortColor });
        }

        public Image Unmosaic()
        {
            if (this.MosaicInfo is null)
            {
                throw new ArgumentException("image is not mosaiced.");
            }

            return this.MosaicInfo.Type switch
            {
                MosaicType.GrayScale => this.UnmosaicGrayScale(),
                MosaicType.FullColor => this.UnmosaicFullColor(),
                MosaicType.Color => this.UnmosaicColor(),
                MosaicType.ShortColor => this.UnmosaicShortColor(),
                _ => throw new ArgumentException("invalid mosaic type."),
            };
        }

        private Image UnmosaicGrayScale()
        {
            var unmosaicedImage = this.Data.CvtColor(ColorConversionCodes.RGBA2GRAY);
            using var mosaiced = this.Data[this.MosaicInfo.Area].Clone();
            using var mosaiced32F =
                new Mat(
                    mosaiced.Rows,
                    mosaiced.Cols,
                    MatType.CV_32F,
                    mosaiced.Data);
            using var unmosaiced = this.UnmosaicImageData(mosaiced32F);
            using var unmosaicedCvt = new Mat();
            unmosaiced.ConvertTo(unmosaicedCvt, this.Data.Type());
            unmosaicedImage[this.MosaicInfo.Area] = unmosaicedCvt;
            return new(unmosaicedImage);
        }

        private Image UnmosaicFull64Color()
        {
            using var unmosaicedImage = this.Data.Clone();
            if (this.MosaicInfo.Scale is null)
            {
                throw new InvalidOperationException("The mosaiced image has no scale information.");
            }

            using var mosaicedArea = unmosaicedImage[this.MosaicInfo.Area];
            using var scaledMosaiced = (mosaicedArea - Scalar.All(this.MosaicInfo.Scale.Beta)) / this.MosaicInfo.Scale.Alpha;
            using var unmosaicedArea = this.UnmosaicImageData(scaledMosaiced);
            unmosaicedImage[this.MosaicInfo.Area] = unmosaicedArea;
            var unmosaicedImage8U = new Mat();
            unmosaicedImage.ConvertTo(unmosaicedImage8U, MatType.CV_8U, 255);
            return new(unmosaicedImage8U);
        }

        private Image UnmosaicFullColor()
        {
            var image64F = new Mat();
            this.Data.ConvertTo(image64F, MatType.CV_64F);
            return new Image(image64F, this.MosaicInfo).UnmosaicFull64Color();
        }

        private Image UnmosaicColor()
        {
            var image64F = new Mat();
            this.Data.ConvertTo(image64F, MatType.CV_64F, 1.0 / 65535);
            return new Image(image64F, this.MosaicInfo).UnmosaicFull64Color();
        }

        private Image UnmosaicShortColor()
        {
            var image64F = new Mat();
            this.Data.ConvertTo(image64F, MatType.CV_64F, 1.0 / 255);
            return new Image(image64F, this.MosaicInfo).UnmosaicFull64Color();
        }

        private Mat MosaicImageData(Mat src)
        {
            var channels = src.Split();
            var mosaiceds = new Mat[src.Channels()];
            for (int i = 0; i < channels.Length; i++)
            {
                mosaiceds[i] = this.DftOneChannel(channels[i]);
            }

            var mosaiced = new Mat();
            Cv2.Merge(mosaiceds, mosaiced);
            return mosaiced;
        }

        private Mat UnmosaicImageData(Mat src)
        {
            var channels = src.Split();
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

        private MosaicScale ScaleImage(Mat mat)
        {
            mat.MinMaxIdx(out var matMin, out var matMax);
            var alpha = 1.0 / (matMax - matMin);
            var beta = -matMin * alpha;
            return new MosaicScale(alpha, beta);
        }
    }
}