using CircleDecodeWebApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace CircleDecodeWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 接收Base64编码格式的图片
        /// </summary>
        [HttpPost]
        public JsonResult Upload()
        {
            //获取base64编码的图片
            //HttpContextBase context = (HttpContextBase)Request.Form["name"];
            string text = Request.Form["name"];

            //获取文件储存路径
            string path = ("C:\\"); //获取当前项目所在目录
            string datetime = GetTimeStamp();
            string suffix = ".jpg"; //文件的后缀名根据实际情况
            string strPath = path + "App_Data\\" + datetime + suffix;

            Debug.WriteLine(strPath);
            //获取图片并保存
            Bitmap resBitmap = Base64ToImg(text.Split(',')[1]);
            resBitmap.Save(strPath);

            JsonResult jr = new JsonResult();
            string resultStr = Decode(strPath);
            jr.Data = resultStr;
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jr;
        }
        /// <summary>
        /// 返回编码好的图片
        /// </summary>
        public ActionResult Getpic(string inputStr)
        {
            Bitmap bmp = Encode(inputStr);
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            bmp.Dispose();
            return File(ms.ToArray(), "image/jpeg"); 
        }

            //解析base64编码获取图片
        private Bitmap Base64ToImg(string base64Code)
        {
            MemoryStream stream = new MemoryStream(Convert.FromBase64String(base64Code));
            return new Bitmap(stream);
        }

        private string Decode(string bitmapPath) {

            return BitmapToStr.PicDecode(bitmapPath);
        }

        /// <summary>
        /// 编码码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Bitmap Encode (string inputStr)
        {

            // 1. 分解字符串

           // string str = "爱笨喵Y";
            string str = inputStr;

            List<string> allDateHexX4List = String2Unicode(str);

            Debug.WriteLine("utf-16的数量：" + allDateHexX4List.Count);

            int sizeOfall = allDateHexX4List.Count;

            int sumCircle = 0;
            int allSizeCanPush = 0;

            List<string> HexForEveCircle = new List<string>();
            int iflag = 0; 
            for (sumCircle = 1; true; sumCircle++)
            {
                allSizeCanPush = allSizeCanPush + sumCircle; 
                // 填充好
                if (sumCircle > 0)
                {
                    string temp = "";
                    for (int i = 0; i < sumCircle; i++) // 一圈 一个增加
                    {
                        if (iflag < allDateHexX4List.Count) // 如果加完了就加 空格
                        {
                            temp += allDateHexX4List[iflag++];
                        }
                        else
                        {
                            temp += "0020";
                        }

                    }
                    HexForEveCircle.Add(temp);
                }

                if (allSizeCanPush >= sizeOfall)
                {
                    break;
                }

            }
            int r = 40; 
            int allR = (sumCircle * 2 + 1) * r; 

            int w = allR * 2;
            int h = allR * 2;

            Bitmap bitmap = new Bitmap(w, h);

            // 可以 开始绘制 了
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            Pen pen = new Pen(Color.Black, 2);


            Rectangle Allrectangle = new Rectangle(0, 0, w, h);
            //// 开始 进入画圈 
            for (int t = HexForEveCircle.Count; t > 0; t--)
            {
                string circleString = HexForEveCircle[t - 1]; 
                string bitString = HexString2BinString(circleString);   
                float bitCount = t * 16; // 这个数量
                Debug.WriteLine(bitString.Count() + "  第 " + t + "圈" + bitString);

                float stepAngle = 360.0f / bitCount;
                float startAngle = 0.0f;

                Rectangle clipRectangle = new Rectangle((sumCircle - t) * 2 * r, (sumCircle - t) * 2 * r
                    , (t * 2 + 1) * r * 2, (t * 2 + 1) * r * 2);  // 确定区域 

                foreach (char c in bitString)
                {
                    Brush brush;

                    if (c.Equals('1'))
                    {
                        brush = Brushes.Black;
                    }
                    else
                    {
                        brush = Brushes.LightGray;
                    }

                    g.FillPie(brush, clipRectangle, startAngle, stepAngle);

                    startAngle = startAngle + stepAngle; // 步进 区域
                }

                Rectangle Crectangle = new Rectangle((sumCircle - t) * 2 * r + r, (sumCircle - t) * 2 * r + r
                    , (t * 2) * r * 2, (t * 2) * r * 2);

                g.FillEllipse(Brushes.White, Crectangle); // 绘制遮盖区域

            }
            Rectangle circleRectangle = new Rectangle(w / 2 - r, h / 2 - r, 2 * r, 2 * r);
            g.FillEllipse(Brushes.Black, circleRectangle); // 绘制中心点

            //绘制水印
            int ht = (int)h / 7;
            Font font = new Font("楷体", ht/5.4f, FontStyle.Regular, GraphicsUnit.Pixel);
            Brush bushBlack = new SolidBrush(Color.Black);//填充的颜色
            g.DrawString("destinylang.com", font, bushBlack
                , h - (1.6f * ht)
                , h - 2 * font.Size
                ); // 画

            return bitmap;


        }




        //获取当前时间段额时间戳
        public string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }


        private static string HexString2BinString(string hexString)
        {
            string result = string.Empty;
            foreach (char c in hexString)
            {
                int v = Convert.ToInt32(c.ToString(), 16);
                int v2 = int.Parse(Convert.ToString(v, 2));
                // 去掉格式串中的空格，即可去掉每个4位二进制数之间的空格，
                result += string.Format("{0:d4}", v2);
            }
            return result;
        }


        /// <summary>
        /// 字符串转Unicode HEX码，x4
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>Unicode编码后的hex字符串 列表 一个元素中四个x4</returns>
        public static List<string> String2Unicode(string source)
        {
            var bytes = Encoding.Unicode.GetBytes(source);
            List<string> tempList = new List<string>();
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("{0:x2}{1:x2}", bytes[i + 1], bytes[i]);
                tempList.Add(stringBuilder.ToString());
                stringBuilder.Clear();
            }
            return tempList;
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}