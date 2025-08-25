using System.Drawing;

namespace PDF_Inspektor
{
    internal static class ImageExtensions
    {
        private const int ExifOrientationTagId = 0x112;

        public static void NormalizeOrientation(this Image image)
        {
            /*
                https://stackoverflow.com/a/23400751/148962

                1 = Horizontal (normal)
                2 = Mirror horizontal
                3 = Rotate 180
                4 = Mirror vertical
                5 = Mirror horizontal and rotate 270 CW
                6 = Rotate 90 CW
                7 = Mirror horizontal and rotate 90 CW
                8 = Rotate 270 CW

            */

            if (Array.IndexOf(image.PropertyIdList, ExifOrientationTagId) > -1)
            {
                int orientation = image.GetPropertyItem(ExifOrientationTagId).Value[0];

                if (orientation is >= 1 and <= 8)
                {
                    switch (orientation)
                    {
                        case 2:
                          image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                          break;

                        case 3:
                          image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                          break;

                        case 4:
                          image.RotateFlip(RotateFlipType.Rotate180FlipX);
                          break;

                        case 5:
                          image.RotateFlip(RotateFlipType.Rotate90FlipX);
                          break;

                        case 6:
                          image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                          break;

                        case 7:
                          image.RotateFlip(RotateFlipType.Rotate270FlipX);
                          break;

                        case 8:
                          image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                          break;
                    }

                    image.RemovePropertyItem(ExifOrientationTagId);
                }
            }
        }
    }
}
