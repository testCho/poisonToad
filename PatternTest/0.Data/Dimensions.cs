using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmallHousing
{
    class Dimensions
    {
        // public field naming: use PascalCase. ex)FindRoadTolerance
        /// 모든 패턴에서 사용 될수 있을만한 전역객체만 저장하는 게 좋을 것 같습니다. ex) 법규 관련 수치, 회사 자체 기준; 최소 방 너비 등
        /// 그 외의 전역객체들은 각 패턴(클래스) 내에서 사용 권장.
        /// Util 처럼 항목별로 관리해야 할 듯 합니다.


        
        // Scale, 스케일
        public static readonly double Scale = 1.0;
        public static readonly double MToMM = 1000 / Scale;
        public static readonly double M2ToMM2 = Math.Pow(MToMM, 2);



        // Road Dectecting, 도로판정
        public static readonly double FindRoadTolerance = 1000 / Scale;
        public static readonly double FindShortSegmentTolerance = 100 / Scale;
        public static readonly double MakeClosedTolerance = 10000 / Scale;
        public static readonly double RoadFindRayLength = 100000 / Scale;



        //Law, 법규
        /// Plot, 대지관련
        public static readonly double NorthThreshold = 9000 / Scale;
        public static readonly double NorthSetBackUnder9m = 1500 / Scale; //9m이하 일조 이격거리
        public static readonly double DSDSurrounding = 1500 / Scale; //공동주택 주변필지 이격거리
        public static readonly double DSDRoad = 1500 / Scale; //공동주택 건축선 이격거리
        public static readonly double DGGSurrounding = 750 / Scale; //단독주택 주변필지 이격거리
        public static readonly double DGGRoad = 1000 / Scale; //단독주택 건축선 이격거리

        /// Parking, 주차관련
        /// 
        public static readonly double BalconyExpensionRate = 1.1; //발코니 확장계수
        public static readonly double CommercialParkingRate = 150.0 * M2ToMM2; //근린생활시설 주차설치기준
        public static readonly double SingleResiParkingThreshold = 150.0 * M2ToMM2; //단독주택 주차설치기준
        public static readonly double SingleResiParkingRate = 100.0 * M2ToMM2; //단독주택 주차설치기준
        public static readonly double Under85ResiParkingRateSpecial = 75.0 * M2ToMM2; //85이하 주거 주차설치기준 (특별시)
        public static readonly double Over85ResiParkingRateSpecial = 65.0 * M2ToMM2; //85초과 주거 주차설치기준 (특별시)


        // Outline, 외곽선
        public static readonly double DetectingVector = 5000 / Scale;
        public static readonly double MinimumUnitLine = 3000 / Scale;
        public static readonly double MinimumUnitArea = 1*M2ToMM2;
        public static readonly double GridWidth = 500 / Scale;
        public static readonly double AcneArea = 1.0 *M2ToMM2;
        public static readonly double FloorHeight = 3000 / Scale;

        public static readonly double MinimumLawLineArea = 60.0 * M2ToMM2;



        // Core, 코어
        public static readonly double CoreWidth = 2400 / Scale;
        public static readonly double CoreHeight = 6150 / Scale;
        public static readonly double LandingLongside = 3360 / Scale;
        public static readonly double LandingShortside = 1600 / Scale;



        // Parking, 주차
        public static readonly double canDoubleLine = 10000 / Scale;
        public static readonly double[] ParkingDepths = { 10000 / Scale, 5000 / Scale, 4000 / Scale, 2000 / Scale };
        
        public static readonly double ClearanceWidth = 2500 / Scale;
        public static readonly double AreaTolerance = 10000 / Scale;
        public static readonly double RoadSetBack = 6000 / Scale;

        public static readonly double parkD = 5000 / Scale;
        public static readonly double parallelwidth = 6000 / Scale;
        public static readonly double parallelheight = 2000 / Scale;
        public static readonly double rightanglewidth = 2300 / Scale;
        public static readonly double rightangleheight = 5000 / Scale;

        public static readonly double GetCut2LengthTolerance = 4000 / Scale;
        public static readonly double RemoveSmallItemLengthTolerance = 2000 / Scale;


        // Corridor, 복도
        public static readonly double OneWayCorridorWidth = 1200.0 / Scale; //편복도 너비
        public static readonly double TwoWayCorridorWidth = 1800.0 / Scale; //중복도 너비
        public static readonly double MinRoomWidth = 3000.0 / Scale; //최소 방너비 (중복도에 수직인 방향으로)


        // Room, 방
        public static readonly double MinRoomArea = 14.0 * M2ToMM2; //원룸 최소면적
        public static readonly double MinCorridorLengthForDoor = 900.0 / Scale; //최소 복도 길이
    }
}
