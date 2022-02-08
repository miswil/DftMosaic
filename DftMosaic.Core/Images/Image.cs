﻿using OpenCvSharp;
using System.Diagnostics;

namespace DftMosaic.Core.Images
{
    public sealed class Image : IDisposable
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

        public Image Mosaic(Rect mosaicRequestArea, double angle, MosaicType mosaicType, bool optimizeSize = true)
        {
            return this.Mosaic(new [] { new MosaicRequestArea(mosaicRequestArea, angle) }, mosaicType, optimizeSize);
        }

        public Image Mosaic(IEnumerable<MosaicRequestArea> mosaicRequestAreas, MosaicType mosaicType, bool optimizeSize = true)
        {
            if (this.MosaicInfo is not null && this.MosaicInfo.Type != mosaicType)
            {
                throw new InvalidOperationException("The specified mosaic type must be same as image mosaic type.");
            }
            return mosaicType switch
            {
                MosaicType.GrayScale => this.MosaicGrayScale(mosaicRequestAreas, optimizeSize),
                MosaicType.FullColor => this.MosaicFullColor(mosaicRequestAreas, optimizeSize),
                MosaicType.Color => this.MosaicColor(mosaicRequestAreas, optimizeSize),
                MosaicType.ShortColor => this.MosaicShortColor(mosaicRequestAreas, optimizeSize),
                _ => throw new ArgumentException("invalid mosaic type."),
            };
        }

        private Image MosaicGrayScale(IEnumerable<MosaicRequestArea> mosaicRequestAreas, bool optimizeSize)
        {
            using var grayedOriginal = this.Data.CvtColor(ColorConversionCodes.BGRA2GRAY);
            using var grayedOriginal32F = new Mat();
            grayedOriginal.ConvertTo(grayedOriginal32F, MatType.CV_32F);
            var mosaicedImage = this.IsMosaiced ?
                this.Data :
                grayedOriginal.CvtColor(ColorConversionCodes.GRAY2BGRA);
            var originalSize = mosaicedImage.Size();
            var mosaicedAreas = new List<MosaicArea>(
                this.MosaicInfo?.Areas ??
                Enumerable.Empty<MosaicArea>());

            foreach (var mosaicRequestArea in mosaicRequestAreas)
            {
                var mosaicArea = this.RotateImage(grayedOriginal32F, mosaicRequestArea.Area, mosaicRequestArea.Angle);
                if (optimizeSize)
                {
                    mosaicArea = this.DftArea(this.Data, mosaicArea, optimizeSize);
                }
                using var mosaiced = this.MosaicImageData(grayedOriginal32F[mosaicArea]);
                using var saveMosaiced = new Mat(mosaiced.Rows, mosaiced.Cols, mosaicedImage.Type(), mosaiced.Data);

                this.RotateImage(mosaicedImage, mosaicRequestArea.Angle);
                mosaicedImage[mosaicArea] = saveMosaiced;
                this.UnrotateImage(mosaicedImage, mosaicRequestArea.Angle, originalSize);
                mosaicedAreas.Add(new(mosaicRequestArea.Area, mosaicRequestArea.Angle, null));
            }

            return new(mosaicedImage, new(MosaicType.GrayScale, mosaicedAreas));
        }

        private Image MosaicFull64Color(IEnumerable<MosaicRequestArea> mosaicRequestAreas, bool optimizeSize)
        {
            var mosaicedAreas = new List<MosaicArea>(
                this.MosaicInfo?.Areas ?? 
                Enumerable.Empty<MosaicArea>());
            var original64F = new Mat();
            var alpha = this.Data.Depth() switch
            {
                0 or 1 => 1.0 / 255,
                2 or 3 => 1.0 / 65535,
                _ => 1.0,
            };
            this.Data.ConvertTo(original64F, MatType.CV_64F, alpha);
            var originalSize = original64F.Size();

            foreach (var mosaicRequestArea in mosaicRequestAreas)
            {
                var mosaicArea = this.RotateImage(original64F, mosaicRequestArea.Area, mosaicRequestArea.Angle);
                if (optimizeSize)
                {
                    mosaicArea = this.DftArea(this.Data, mosaicArea, optimizeSize);
                }

                using var mosaiced = this.MosaicImageData(original64F[mosaicArea]);
                var scale = this.ScaleImage(mosaiced);
                original64F[mosaicArea] = mosaiced * scale.Alpha + Scalar.All(scale.Beta);
                this.UnrotateImage(original64F, mosaicRequestArea.Angle, originalSize);
                mosaicedAreas.Add(new(mosaicRequestArea.Area, mosaicRequestArea.Angle, scale));
            }
            return new(original64F, new(MosaicType.Full64Color, mosaicedAreas));
        }

        private Image MosaicFullColor(IEnumerable<MosaicRequestArea> mosaicRequestAreas, bool optimizeSize)
        {
            using var fullColor = this.MosaicFull64Color(mosaicRequestAreas, optimizeSize);
            var mosaiced32F = new Mat();
            fullColor.Data.ConvertTo(mosaiced32F, MatType.CV_32F);
            return new(mosaiced32F, fullColor.MosaicInfo with { Type = MosaicType.FullColor });
        }

        private Image MosaicColor(IEnumerable<MosaicRequestArea> mosaicRequestAreas, bool optimizeSize)
        {
            using var fullColor = this.MosaicFull64Color(mosaicRequestAreas, optimizeSize);
            var mosaiced16U = new Mat();
            fullColor.Data.ConvertTo(mosaiced16U, MatType.CV_16U, 65535);
            return new(mosaiced16U, fullColor.MosaicInfo with { Type = MosaicType.Color });
        }

        private Image MosaicShortColor(IEnumerable<MosaicRequestArea> mosaicRequestAreas, bool optimizeSize)
        {
            using var fullColor = this.MosaicFull64Color(mosaicRequestAreas, optimizeSize);
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
            var mosaicedImage = this.Data.Clone();
            foreach (var area in this.MosaicInfo.Areas.Reverse())
            {
                var mosaicArea = this.RotateImage(mosaicedImage, area.Area, area.Angle);
                this.RotateImage(unmosaicedImage, area.Angle);

                using var mosaiced = mosaicedImage[mosaicArea].Clone();
                using var mosaiced32F =
                    new Mat(
                        mosaiced.Rows,
                        mosaiced.Cols,
                        MatType.CV_32F,
                        mosaiced.Data);
                using var unmosaiced = this.UnmosaicImageData(mosaiced32F);
                using var unmosaicedCvt = new Mat();
                unmosaiced.ConvertTo(unmosaicedCvt, this.Data.Type());
                unmosaicedImage[mosaicArea] = unmosaicedCvt;

                this.UnrotateImage(mosaicedImage, area.Angle, this.Data.Size());
                this.UnrotateImage(unmosaicedImage, area.Angle, this.Data.Size());
            }
            return new(unmosaicedImage);
        }

        private Image UnmosaicFull64Color()
        {
            using var unmosaicedImage = this.Data.Clone();
            foreach (var area in this.MosaicInfo.Areas.Reverse())
            {
                if (area.Scale is null)
                {
                    throw new InvalidOperationException("The mosaiced image has no scale information.");
                }

                var mosaicArea = this.RotateImage(unmosaicedImage, area.Area, area.Angle);

                using var mosaicedArea = unmosaicedImage[mosaicArea];
                using var scaledMosaiced = (mosaicedArea - Scalar.All(area.Scale.Beta)) / area.Scale.Alpha;
                using var unmosaicedArea = this.UnmosaicImageData(scaledMosaiced);
                unmosaicedImage[mosaicArea] = unmosaicedArea;

                this.UnrotateImage(unmosaicedImage, area.Angle, this.Data.Size());
            }
            var unmosaicedImage8U = new Mat();
            unmosaicedImage.ConvertTo(unmosaicedImage8U, MatType.CV_8U, 255);
            return new(unmosaicedImage8U);
        }

        private Image UnmosaicFullColor()
        {
            using var image64F = new Mat();
            this.Data.ConvertTo(image64F, MatType.CV_64F);
            return new Image(image64F, this.MosaicInfo).UnmosaicFull64Color();
        }

        private Image UnmosaicColor()
        {
            using var image64F = new Mat();
            this.Data.ConvertTo(image64F, MatType.CV_64F, 1.0 / 65535);
            return new Image(image64F, this.MosaicInfo).UnmosaicFull64Color();
        }

        private Image UnmosaicShortColor()
        {
            using var image64F = new Mat();
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

        private void RotateImage(Mat image, double angle)
        {
            this.RotateImage(image, default, angle);
        }

        private Rect RotateImage(Mat image, Rect mosaicedArea, double angle)
        {
            var radAngle = angle / 360 * 2 * Math.PI;
            using var rotateMat = Cv2.GetRotationMatrix2D(new Point2f(image.Width / 2.0f, image.Height / 2.0f), angle, 1.0);
            var rotatedWidth = (int)Math.Round(image.Width * Math.Abs(Math.Cos(radAngle)) + image.Height * Math.Abs(Math.Sin(radAngle)));
            var rotatedHeight = (int)Math.Round(image.Width * Math.Abs(Math.Sin(radAngle)) + image.Height * Math.Abs(Math.Cos(radAngle)));
            rotateMat.Set(0, 2, rotateMat.At<double>(0, 2) + (rotatedWidth - image.Width) / 2.0);
            rotateMat.Set(1, 2, rotateMat.At<double>(1, 2) + (rotatedHeight - image.Height) / 2.0);

            using var imageClone = image.Clone();
            Cv2.WarpAffine(imageClone, image, rotateMat, new Size(rotatedWidth, rotatedHeight));

            using var areaPosition = new Mat(3, 1, MatType.CV_64F, new double[] { mosaicedArea.X, mosaicedArea.Y, 1.0 });
            using var areaPositionRotateMat = Cv2.GetRotationMatrix2D(new(mosaicedArea.X + mosaicedArea.Width / 2.0f, mosaicedArea.Y + mosaicedArea.Height / 2.0f), -angle, 1.0);
            using var areaPositionRotated = (areaPositionRotateMat * areaPosition).ToMat();
            using var rotatedAreaPosition = new Mat(3, 1, MatType.CV_64F, new double[] { areaPositionRotated.At<double>(0), areaPositionRotated.At<double>(1), 1.0 });
            using var rotatedRotatedAreaPosition = (rotateMat * rotatedAreaPosition).ToMat();

            return new Rect((int)rotatedRotatedAreaPosition.At<double>(0), (int)rotatedRotatedAreaPosition.At<double>(1), mosaicedArea.Width, mosaicedArea.Height);
        }

        private void UnrotateImage(Mat image, double angle, Size originalSize)
        {
            using var rotateMatInv = Cv2.GetRotationMatrix2D(new(image.Width / 2.0f, image.Height / 2.0f), -angle, 1.0);
            rotateMatInv.Set(0, 2, rotateMatInv.At<double>(0, 2) + (originalSize.Width - image.Width) / 2.0);
            rotateMatInv.Set(1, 2, rotateMatInv.At<double>(1, 2) + (originalSize.Height - image.Height) / 2.0);
            
            using var imageClone = image.Clone();
            Cv2.WarpAffine(imageClone, image, rotateMatInv, new Size(originalSize.Width, originalSize.Height));
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

        public void Dispose()
        {
            this.Data.Dispose();
        }
    }
}