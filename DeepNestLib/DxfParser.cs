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
    public class DxfParser
    {
        private const float SCALE = 1;
        private static readonly List<DxfEntityType> _allowedEntites = new() { DxfEntityType.Polyline, DxfEntityType.LwPolyline, DxfEntityType.Circle };
        private static readonly List<string> _allowedLayers = new() { "Outside", "OutsideNT" };
        public static RawDetail loadDxf(string path)
        {
            FileInfo fi = new FileInfo(path);
            DxfFile dxf = DxfFile.Load(fi.FullName);
            RawDetail s = new RawDetail();

            //used to replace the dxf on a nested sheet 
            s.Name = fi.FullName;

            //for now only store used types less for each iterations required
            IEnumerable<DxfEntity> entities = dxf.Entities.Where(e => _allowedEntites.Contains(e.EntityType) && _allowedLayers.Contains(e.Layer));

            foreach (DxfEntity ent in entities)
            {
                LocalContour contour = new LocalContour();

                switch (ent.EntityType)
                {
                    case DxfEntityType.LwPolyline:
                        {

                            DxfLwPolyline poly = (DxfLwPolyline)ent;
                            if (!poly.IsClosed)
                            {
                                continue;
                            }
                            foreach (DxfLwPolylineVertex vert in poly.Vertices)
                            {
                                contour.Points.Add(new PointF((float)vert.X * SCALE, (float)vert.Y * SCALE));
                            }
                            break;
                        }

                    case DxfEntityType.Polyline:
                        {

                            DxfPolyline poly = (DxfPolyline)ent;
                            if (!poly.IsClosed)
                            {
                                continue;
                            }

                            foreach (DxfVertex vert in poly.Vertices)
                            {
                                contour.Points.Add(new PointF((float)vert.Location.X * SCALE, (float)vert.Location.Y * SCALE));

                            }

                            break;
                        }

                };


                if (contour.Points.Count() < 3)
                {
                    continue;
                }
                s.Outers.Add(contour);


            }


            return s;
        }

        public static int Export(string path, IEnumerable<NFP> polygons, IEnumerable<NFP> sheets)
        {
            Dictionary<DxfFile, int> dxfexports = new Dictionary<DxfFile, int>();

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
                //TR Point
                sheetverts.Add(new DxfVertex(new DxfPoint(sheetwidth, sheetheight, 0)));
                //TL Point
                sheetverts.Add(new DxfVertex(new DxfPoint(0, sheetheight, 0)));


                DxfPolyline sheetentity = new DxfPolyline(sheetverts)
                {

                    IsClosed = true,
                    Layer = $"Plate H{sheetheight} W{sheetwidth}",

                };


                sheetdxf.Entities.Add(sheetentity);

                foreach (NFP nFP in polygons)
                {

                    DxfFile fl;
                    if (nFP.fitted == false || !nFP.Name.ToLower().Contains(".dxf") || nFP.sheet.id != sheets.ElementAt(i).id)
                    {
                        continue;
                    }
                    else
                    {
                        fl = DxfFile.Load(nFP.Name);
                    }

                    double sheetXoffset = -(sheetwidth + NestingContext.GAP_BETWEEN_SHEETS) * i;
                    //double sheetyoffset = -sheetheight * i;
                    DxfPoint offsetdistance = new DxfPoint(nFP.x + sheetXoffset, nFP.y, 0D);
                    List<DxfEntity> newlist = OffsetToNest(fl.Entities, offsetdistance, nFP.Rotation);

                    foreach (DxfEntity ent in newlist)
                    {
                        sheetdxf.Entities.Add(ent);
                    }


                }


                dxfexports.Add(sheetdxf, sheets.ElementAt(i).id);


            }

            var timestamp = DateTime.Now.ToString("MMddHHmm");
            int sheetcount = 0;
            for (int i = 0; i < dxfexports.Count(); i++)
            {

                var dxf = dxfexports.ElementAt(i).Key;
                var id = dxfexports.ElementAt(i).Value;

                if (dxf.Entities.Count != 1)
                {
                    sheetcount += 1;
                    dxf.Save(Path.Combine(path, $"{id}.dxf"), true);
                }


            }
            return sheetcount;
        }

        static private List<DxfEntity> OffsetToNest(IList<DxfEntity> dxfEntities, DxfPoint offset, Double RotationAngle)
        {

            List<DxfEntity> dxfreturn = new List<DxfEntity>();


            foreach (DxfEntity entity in dxfEntities)
            {

                switch (entity.EntityType)
                {
                    case DxfEntityType.Arc:
                        {
                            DxfArc dxfArc = (DxfArc)entity;
                            dxfArc.Center += RotateLocation(RotationAngle, dxfArc.Center);
                            dxfArc.Center += offset;
                            dxfArc.StartAngle += RotationAngle;
                            dxfArc.EndAngle += RotationAngle;
                            dxfreturn.Add(dxfArc);
                            break;
                        }

                    case DxfEntityType.ArcAlignedText:
                        {
                            DxfArcAlignedText dxfArcAligned = (DxfArcAlignedText)entity;
                            dxfArcAligned.CenterPoint = RotateLocation(RotationAngle, dxfArcAligned.CenterPoint);
                            dxfArcAligned.CenterPoint += offset;
                            dxfArcAligned.StartAngle += RotationAngle;
                            dxfArcAligned.EndAngle += RotationAngle;
                            dxfreturn.Add(dxfArcAligned);
                            break;
                        }

                    case DxfEntityType.Attribute:
                        {
                            DxfAttribute dxfAttribute = (DxfAttribute)entity;
                            dxfAttribute.Location = RotateLocation(RotationAngle, dxfAttribute.Location);
                            dxfAttribute.Location += offset;
                            dxfreturn.Add(dxfAttribute);
                            break;

                        }
                    case DxfEntityType.AttributeDefinition:
                        {
                            DxfAttributeDefinition dxfAttributecommon = (DxfAttributeDefinition)entity;
                            dxfAttributecommon.Location = RotateLocation(RotationAngle, dxfAttributecommon.Location);
                            dxfAttributecommon.Location += offset;
                            dxfreturn.Add(dxfAttributecommon);
                            break;
                        }

                    case DxfEntityType.Body:
                        throw new NotImplementedException();

                    case DxfEntityType.Circle:
                        {
                            DxfCircle dxfCircle = (DxfCircle)entity;
                            dxfCircle.Center = RotateLocation(RotationAngle, dxfCircle.Center);
                            dxfCircle.Center += offset;
                            dxfreturn.Add(dxfCircle);
                            break;
                        }

                    case DxfEntityType.DgnUnderlay:
                        throw new NotImplementedException();

                    case DxfEntityType.Dimension:
                        throw new NotImplementedException();


                    case DxfEntityType.DwfUnderlay:
                        throw new NotImplementedException();

                    case DxfEntityType.Ellipse:
                        {
                            DxfEllipse dxfEllipse = (DxfEllipse)entity;
                            dxfEllipse.Center = RotateLocation(RotationAngle, dxfEllipse.Center);
                            dxfEllipse.Center += offset;
                            dxfreturn.Add(dxfEllipse);
                            break;
                        }

                    case DxfEntityType.Face:
                        throw new NotImplementedException();


                    case DxfEntityType.Helix:
                        throw new NotImplementedException();


                    case DxfEntityType.Image:
                        {
                            DxfImage dxfImage = (DxfImage)entity;
                            dxfImage.Location = RotateLocation(RotationAngle, dxfImage.Location);
                            dxfImage.Location += offset;

                            dxfreturn.Add(dxfImage);
                            break;
                        }
                    case DxfEntityType.Insert:
                        throw new NotImplementedException();

                    case DxfEntityType.Leader:
                        {
                            DxfLeader dxfLeader = (DxfLeader)entity;
                            List<DxfPoint> pts = new List<DxfPoint>();

                            foreach (DxfPoint vrt in dxfLeader.Vertices)
                            {
                                var tmppnt = RotateLocation(RotationAngle, vrt);
                                tmppnt += offset;
                                pts.Add(tmppnt);
                            }


                            dxfLeader.Vertices.Clear();
                            dxfLeader.Vertices.Concat(pts);
                            dxfreturn.Add(dxfLeader);
                            break;
                        }
                    case DxfEntityType.Light:
                        throw new NotImplementedException();


                    case DxfEntityType.Line:
                        {
                            DxfLine dxfLine = (DxfLine)entity;
                            dxfLine.P1 = RotateLocation(RotationAngle, dxfLine.P1);
                            dxfLine.P2 = RotateLocation(RotationAngle, dxfLine.P2);
                            dxfLine.P1 += offset;
                            dxfLine.P2 += offset;
                            dxfreturn.Add(dxfLine);

                            break;
                        }
                    case DxfEntityType.LwPolyline:
                        {
                            DxfPolyline dxfPoly = (DxfPolyline)entity;
                            foreach (DxfVertex pts in dxfPoly.Vertices)
                            {
                                pts.Location = RotateLocation(RotationAngle, pts.Location);
                                pts.Location += offset;
                            }

                            dxfreturn.Add(dxfPoly);
                            break;
                        }
                    case DxfEntityType.MLine:
                        {
                            DxfMLine mLine = (DxfMLine)entity;
                            List<DxfPoint> pts = new List<DxfPoint>();
                            mLine.StartPoint += offset;

                            mLine.StartPoint = RotateLocation(RotationAngle, mLine.StartPoint);

                            foreach (DxfPoint vrt in mLine.Vertices)
                            {
                                var tmppnt = RotateLocation(RotationAngle, vrt);
                                tmppnt += offset;
                                pts.Add(tmppnt);
                            }
                            mLine.Vertices.Clear();
                            mLine.Vertices.Concat(pts);
                            dxfreturn.Add(mLine);
                            break;
                        }

                    case DxfEntityType.Polyline:
                        {
                            DxfPolyline polyline = (DxfPolyline)entity;

                            List<DxfVertex> verts = new List<DxfVertex>();
                            foreach (DxfVertex vrt in polyline.Vertices)
                            {
                                var tmppnt = vrt;
                                tmppnt.Location = RotateLocation(RotationAngle, tmppnt.Location);
                                tmppnt.Location += offset;
                                verts.Add(tmppnt);
                            }
                            DxfPolyline polyout = new DxfPolyline(verts);
                            polyout.Location = polyline.Location + offset;
                            polyout.IsClosed = polyline.IsClosed;
                            polyout.Layer = polyline.Layer;
                            dxfreturn.Add(polyout);

                            break;
                        }



                    case DxfEntityType.ProxyEntity:
                    case DxfEntityType.Ray:
                    case DxfEntityType.Region:
                    case DxfEntityType.RText:
                    case DxfEntityType.Section:
                    case DxfEntityType.Seqend:
                    case DxfEntityType.Shape:
                    case DxfEntityType.Solid:
                    case DxfEntityType.Spline:
                    case DxfEntityType.Text:
                    case DxfEntityType.Tolerance:
                    case DxfEntityType.Trace:
                    case DxfEntityType.Underlay:
                    case DxfEntityType.Vertex:
                    case DxfEntityType.WipeOut:
                    case DxfEntityType.XLine:
                    case DxfEntityType.Ole2Frame:
                    case DxfEntityType.OleFrame:
                    case DxfEntityType.PdfUnderlay:
                    case DxfEntityType.ModelerGeometry:
                    case DxfEntityType.Point:
                    case DxfEntityType.MText:
                        //throw new NotImplementedException();
                        break;









                }

            }
            return dxfreturn;
        }

        public static DxfPoint RotateLocation(double RotationAngle, DxfPoint Pt)
        {
            var angle = (float)(RotationAngle * Math.PI / 180.0f);
            var x = Pt.X;
            var y = Pt.Y;
            var x1 = (float)(x * Math.Cos(angle) - y * Math.Sin(angle));
            var y1 = (float)(x * Math.Sin(angle) + y * Math.Cos(angle));
            return new DxfPoint(x1, y1, Pt.Z);
        }

        public static List<DxfPoint> RotateLocation(double RotationAngle, List<DxfPoint> Pts)
        {
            List<DxfPoint> PtsRet = new List<DxfPoint>();
            var angle = (float)(RotationAngle * Math.PI / 180.0f);
            for (var i = 0; i < Pts.Count; i++)
            {
                var x = Pts[1].X;
                var y = Pts[i].Y;
                var x1 = (float)(x * Math.Cos(angle) - y * Math.Sin(angle));
                var y1 = (float)(x * Math.Sin(angle) + y * Math.Cos(angle));
                PtsRet.Add(new DxfPoint(x1, y1, Pts[i].Z));
            }
            return PtsRet;
        }

    }

}

