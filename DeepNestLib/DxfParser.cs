using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf;
using System.Drawing;
using System.IO;

namespace DeepNestLib
{
    class DxfParser
    {
        public static RawDetail loadDxf(string path)
        {
            FileInfo fi = new FileInfo(path);
            DxfFile dxffile = DxfFile.Load(fi.FullName);
            RawDetail s = new RawDetail();
            s.Name = fi.Name;

            //for now only store used types less for each iterations required
            IEnumerable<DxfEntity> entities = dxffile.Entities.Where(ent => ent.EntityType == DxfEntityType.Polyline || ent.EntityType == DxfEntityType.LwPolyline );
            
            foreach (DxfEntity ent in entities)
            {
                LocalContour points = new LocalContour();

                switch(ent.EntityType)
                {
                    case DxfEntityType.LwPolyline:
                    {
                            DxfLwPolyline poly = (DxfLwPolyline)ent;
                            foreach (DxfLwPolylineVertex vert in poly.Vertices)
                            {
                                points.Points.Add(new PointF((float)vert.X, (float)vert.Y));
                            }
                            break;
                    }

                    case DxfEntityType.Polyline:
                        {
                            DxfPolyline poly = (DxfPolyline)ent;
                            foreach (DxfVertex vert in poly.Vertices)
                            {
                                points.Points.Add(new PointF((float)vert.Location.X, (float)vert.Location.Y));
                                
                            }

                            break;
                        }

                };
                s.Outers.Add(points);
                
            }
            

            return s;
        }
    }

}
