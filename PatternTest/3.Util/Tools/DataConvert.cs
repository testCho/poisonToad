using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallHousing.Utility
{
    class DataConvert
    {
        public static string UsageConverter(string usagecode)
        {
            switch (int.Parse(usagecode))
            {
                case 11:
                    return "1종전용";
                case 13:
                    return "1종일반";
                case 14:
                    return "2종일반";
                case 15:
                    return "3종일반";
                case 16:
                    return "준주거";
                case 22:
                    return "일반상업";
                case 42:
                    return "보전녹지";
                case 43:
                    return "자연녹지";
                case 44:
                    return "생산녹지";
                default:
                    return "알수없음";
            }
        }
        public static string JimokConverter(string jimokcode)
        {
            switch (int.Parse(jimokcode))
            {
                case 1:
                    return "전";
                case 2:
                    return "답";
                case 3:
                    return "과수원";
                case 4:
                    return "목장용지";
                case 5:
                    return "임야";
                case 6:
                    return "광천지";
                case 7:
                    return "염전";
                case 8:
                    return "대";
                case 9:
                    return "공장용지";
                case 10:
                    return "학교용지";
                case 11:
                    return "주차장";
                case 12:
                    return "주유소용지";
                case 13:
                    return "창고용지";
                case 14:
                    return "도로";
                case 15:
                    return "철도용지";
                case 16:
                    return "제방";
                case 17:
                    return "하천";
                case 18:
                    return "구거";
                case 19:
                    return "유지";
                case 20:
                    return "양어장";
                case 21:
                    return "수도용지";
                case 22:
                    return "공원";
                case 23:
                    return "체육용지";
                case 24:
                    return "유원지";
                case 25:
                    return "종교용지";
                case 26:
                    return "사적지";
                case 27:
                    return "묘지";
                case 28:
                    return "잡종지";
                default:
                    return "알 수 없음";
            }
        }
    }
}
