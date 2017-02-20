using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

using SmallHousing.Utility;

namespace SmallHousing
{
    public class Floor
    {
        private Polyline outline = new Polyline();
        private List<Polyline> corridors = new List<Polyline>();
        private List<Core> cores = new List<Core>();
        private List<Room> rooms = new List<Room>();

        public Floor()
        { }

        public Floor(Floor copy)
        {
            this.outline = copy.Outline;
            this.corridors = copy.Corridors;
            this.cores = copy.cores.Select(n => new Core(n)).ToList();
            this.rooms = copy.rooms.Select(n => new Room(n)).ToList();
        }

        public Polyline Outline { get { return outline; } set { outline = value as Polyline; } }
        public List<Polyline> Corridors { get { return corridors; } set { corridors = value as List<Polyline>; } }
        public List<Core> Cores { get { return cores; } set { cores = value as List<Core>; } }
        public List<Room> Rooms { get { return rooms; } set { rooms = value as List<Room>; } }
    }

    public class Core
    {
        private Polyline outline = new Polyline();

        public Core()
        { }

        public Core(Core otherCore)
        {
            this.Outline = new Polyline(otherCore.Outline);
            this.Landing = otherCore.Landing;
        }

        public Polyline Outline { get { return outline; } set { outline = value as Polyline; } } //외곽선


        // 구버전 호환용 임시구조
        private Polyline landing = new Polyline();
        public Polyline Landing { get { return landing; } set { landing = value as Polyline; } }

        public Core(Polyline outline, Polyline landing)
        {
            this.outline = outline;
            this.landing = landing;
        }
    }

    public class Room
    {
        private Polyline outline = new Polyline();
        private double area = new double();
        private Polyline exclusive = new Polyline();
        private Polyline service = new Polyline();

        public Room(double area)
        {
            this.Area = area;
        }

        public Room()
        { }

        public Room(Room otherRoom)
        {
            this.Outline = new Polyline(otherRoom.Outline);
            this.Area = otherRoom.Area;
            this.Exclusive = new Polyline(otherRoom.Exclusive);
            this.Service = new Polyline(otherRoom.Service);
        }

        public Polyline Outline { get { return outline; } set { outline = value as Polyline; } } //외곽선
        public double Area { get { return area; } set { area = value; } } //면적
        public Polyline Exclusive { get { return exclusive; } set { exclusive = value as Polyline; } } //전용면적
        public Polyline Service { get { return service; } set { service = value as Polyline; } } //서비스면적
    }

}
