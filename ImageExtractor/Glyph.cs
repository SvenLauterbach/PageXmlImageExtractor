using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageExtractor
{
    public class Glyph
    {
        public Glyph()
        {
            Points = new List<Point>();
        }

        private string _pointsString;

        public string ID { get; set; }
        public string Unicode { get; set; }

        public string PointsString
        {
            get
            {
                return _pointsString;
            }
            set
            {
                //Todo: Refactor with some regex magic
                _pointsString = value;
                string[] points = value.Split(' ');

                foreach (var point in points)
                {
                    string[] coords = point.Split(',');
                    Point p = new Point(Convert.ToInt32(coords[0]), Convert.ToInt32(coords[1]));
                    Points.Add(p);
                }
            }
        }

        public List<Point> Points { get; set; }
    }
}
