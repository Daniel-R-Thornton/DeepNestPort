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
            
            //used to replace the dxf on a nested sheet 
            s.Name = fi.FullName;

            //for now only store used types less for each iterations required
            IEnumerable<DxfEntity> entities = dxffile.Entities.Where(ent => ent.EntityType == DxfEntityType.Polyline || ent.EntityType == DxfEntityType.LwPolyline);

            foreach (DxfEntity ent in entities)
            {
                LocalContour points = new LocalContour();

                switch (ent.EntityType)
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
        public static void Export(string path, IEnumerable<NFP> polygons, IEnumerable<NFP> sheets)
        {
            Dictionary<DxfFile,int> dxfexports = new Dictionary<DxfFile, int>();

            for (int i = 0; i < sheets.Count(); i++)
            {
                //Generate Sheet Outline in Dxf

                DxfFile sheetdxf = new DxfFile();
                sheetdxf.Views.Clear();

                List<DxfVertex> sheetverts = new List<DxfVertex>();
                double sheetheight = sheets.ElementAt(i).HeightCalculated;
                double sheetwidth = sheets.ElementAt(i).WidthCalculated;

                //Bl Point
                sheetverts.Add(new DxfVertex(new DxfPoint(0, 0, 0)));
                //BR Point
                sheetverts.Add(new DxfVertex(new DxfPoint(sheetwidth, 0, 0)));
                //TL Point
                sheetverts.Add(new DxfVertex(new DxfPoint(0, sheetheight, 0)));
                //TR Point
                sheetverts.Add(new DxfVertex(new DxfPoint(sheetwidth, sheetheight, 0)));
                DxfPolyline sheetentity = new DxfPolyline(sheetverts)
                {
                    IsClosed = true,
                    Layer = $"Plate H{sheetheight} W{sheetwidth}",
                };

                sheetdxf.Entities.Add(sheetentity);
                dxfexports.Add(sheetdxf, sheets.ElementAt(i).id);

                    
            }
            
        }

    }

}
