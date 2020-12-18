using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace CircleDecodeWebApp.Models
{
    public class BitmapToStr
    {
        /// <summary>
        /// 解码图片
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static string PicDecode(string bitmapPath) {

            return Run(bitmapPath);
        }
        private static string Run(string bitmapPath)
        {
            //读取图片
            var img = Cv2.ImRead(bitmapPath);
            Mat gray = img.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat ThresholdImg = gray.Threshold(100, 255, ThresholdTypes.Binary);
            Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
            Mat openImg = ThresholdImg.MorphologyEx(MorphTypes.Open, element);
            int x = 0, y = 0, w = openImg.Width, h = openImg.Height;
            Rect roi = new Rect(x, y, w, h);
            Mat ROIimg = new Mat(openImg, roi);

            //寻找图像轮廓
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierachy;
            Cv2.FindContours(openImg, out contours, out hierachy, RetrievalModes.List, ContourApproximationModes.ApproxTC89KCOS);

            float Center_X = 0;
            float Center_Y = 0;
            float Radius = 0;

            for (int i = 0; i < contours.Length; i++)
            {
                if (contours[i].Length < 5) continue;
                var rrt = Cv2.FitEllipse(contours[i]);
                if ((Math.Abs(rrt.Size.Height / (rrt.Size.Height + rrt.Size.Width) - 0.5)) < 0.001
                     && (Math.Abs((rrt.Center.X / (rrt.Center.X + rrt.Center.Y)) - 0.5)) < 0.01) { 
                    rrt.Center.X += x;
                    rrt.Center.Y += y;
                    Center_X = rrt.Center.X;
                    Center_Y = rrt.Center.Y;
                    Radius = (rrt.Size.Height + rrt.Size.Width) / 8 - 0.28f;
                    break;

                }

            }
            if (Radius < 1)
            {
                return "图片格式错误！";
            }

            string resultStr = ""; // 定义结果
            for (int i = 1; true; i++) // 一圈         
            {
                // 寻找字符
                int allBit = i * 16;
                float stepAngle = 360.0f / allBit;
                List<int> LightSpotPerCircleList = new List<int>();

                for (int j = 0; j < allBit; j++)
                {
                    
                    double angle1 = j * stepAngle / 180 * Math.PI;
                    double angle2 = (j * stepAngle + stepAngle) / 180 * Math.PI;

                    double len1 = Radius * (i * 2) * 2;
                    double len2 = Radius * (2 * i + 1) * 2;

                    double x1 = Center_X + len1 * Math.Cos(angle1);
                    double x2 = Center_X + len1 * Math.Cos(angle2);
                    double x3 = Center_X + len2 * Math.Cos(angle1);
                    double x4 = Center_X + len2 * Math.Cos(angle2);


                    double y1 = Center_Y + len1 * Math.Sin(angle1);
                    double y2 = Center_Y + len1 * Math.Sin(angle2);
                    double y3 = Center_Y + len2 * Math.Sin(angle1);
                    double y4 = Center_Y + len2 * Math.Sin(angle2);


                    double minX = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
                    double MaxX = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));

                    double minY = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
                    double MaxY = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));
                    

                    Mat mask = Mat.Zeros(img.Size(), MatType.CV_8UC1); 
                    Cv2.Ellipse(mask
                        , new OpenCvSharp.Point((int)(Center_X), (int)(Center_Y))
                       , new OpenCvSharp.Size((Radius * (2 * i + 1) * 2), (Radius * (2 * i + 1) * 2))
                        , 0
                        , j * stepAngle, j * stepAngle + stepAngle
                        , new Scalar(255, 255, 255), -1);
                    Cv2.Ellipse(mask
                        , new OpenCvSharp.Point((int)(Center_X), (int)(Center_Y))
                        , new OpenCvSharp.Size((Radius * (i * 2) * 2), (Radius * (i * 2) * 2))
                        , 0
                        , j * stepAngle, j * stepAngle + stepAngle
                        , new Scalar(0, 0, 0), -1);
                    // 
                    Mat imgL = Mat.Zeros(img.Size(), MatType.CV_8UC1); // 来一个空白图
                    img.CopyTo(imgL, mask);
                    // 统计数量,并记录
                    int count = bSums(imgL, (int)minX, (int)MaxX, (int)minY, (int)MaxY);
                   // int count = 0;
                    LightSpotPerCircleList.Add(count);
                    mask.Dispose();
                    imgL.Dispose();
                }
                // 走完一圈后，进行分析， 解码
                int midValue = (LightSpotPerCircleList.Max() + LightSpotPerCircleList.Min()) / 2;
                if (LightSpotPerCircleList.Max()- LightSpotPerCircleList.Min() < 2)
                {
                   continue;// 最后一圈就不要了,相差太小了
                }


                int intFlag = 0;
                string strBinary = "";

                foreach (int count in LightSpotPerCircleList) // 一圈的解码
                {
                    intFlag++;
                    if (count > midValue)
                    {
                        strBinary += "0";
                    }
                    else
                    {
                        strBinary += "1";
                    }
                    if (intFlag % 16 == 0)// 每16 个 读一个出来
                    {
                       // Debug.WriteLine("将要解码的二进制" + strBinary);
                        resultStr += Binary2Unicode(strBinary);// 得出一个解码一个
                        strBinary = "";
                    }
                }
                Debug.WriteLine("resultStr:" + resultStr);
 
                if ((2 * i + 1) * Radius * 2 >= ((img.Size().Width + img.Size().Height) / 4))
                {
                   // Debug.WriteLine("判断退出否？" + resultStr);
                    break;
                }

            }

            Debug.WriteLine("解码完成：" + resultStr);
            return resultStr;
        }

        /// <summary>
        /// bitmap 位图转为mat类型 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Mat Bitmap2Mat(Bitmap bitmap)
        {
            MemoryStream s2_ms = null;
            Mat source = null;
            try
            {
                using (s2_ms = new MemoryStream())
                {
                    bitmap.Save(s2_ms, ImageFormat.Bmp);
                    source = Mat.FromStream(s2_ms, ImreadModes.AnyColor);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
            finally
            {
                if (s2_ms != null)
                {
                    s2_ms.Close();
                    s2_ms = null;
                }
                GC.Collect();
            }
            return source;
        }


        /// <summary>    
        /// 将二进制 “10011100000000011100011111111101” 转成 字符串    
        /// </summary>    
        /// <param name="s">16位的二进制</param>    
        /// <returns></returns>    
        private static string Binary2Unicode(string s)
        {
            string str_hex = string.Format("{0:x4}", Convert.ToInt32(s, 2));

            string str_hex2 = String.Format("\\u{0:X4}", str_hex);

            return UnicodeToString(str_hex2);

        }

        /// <summary>  
        /// Unicode字符串转为正常字符串
        /// </summary>  
        /// <param name="srcText"></param>  
        /// <returns></returns>  
        private static string UnicodeToString(string srcText)
        {
            string dst = "";
            string src = srcText;
            int len = srcText.Length / 6;
            for (int i = 0; i <= len - 1; i++)
            {
                string str = "";
                str = src.Substring(0, 6).Substring(2);
                src = src.Substring(6);
                byte[] bytes = new byte[2];
                bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                dst += Encoding.Unicode.GetString(bytes);
            }
            return dst;
        }

        /// <summary>
        /// 获取二值化图像中的亮点数量
        /// </summary>
        /// <param name="src">图像</param>
        /// <returns></returns>
        private static int bSums(Mat src, int minX, int MaxX, int minY, int MaxY)
        {
            int sidelen = 4; 
            minX = minX + ((MaxX - minX - sidelen) / 2);
            MaxX = minX + sidelen;

            minY = minY + ((MaxY - minY - sidelen) / 2);
            MaxY = minY + sidelen;

            int count = 0;
            for (int i = minY; i < MaxY; i++)
            {
                for (int j = minX; j < MaxX; j++)
                {
                    if (i >= 0 && j >= 0 && i < src.Rows && j < src.Cols)
                    {
                        char r = src.At<char>(i, j);

                        //   Debug.WriteLine("读出来的r值为：" + r);

                        if (r > 1)
                        {
                            count++;
                        }
                    }

                }
            }
            return count;
        }



    }
}