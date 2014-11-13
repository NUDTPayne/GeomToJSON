//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

public partial class UserDefinedFunctions
{
    [Microsoft.SqlServer.Server.SqlFunction]
    public static String GeomToJSON(SqlGeometry geom)
    {
        if (geom.IsNull) return "";

        String GeometryType = geom.STGeometryType().Value;
        try { 
        if (GeometryType == "Point" || GeometryType == "MultiPoint") return PointsToJSON(geom);

        //Ensure we have valid geometry
        geom = geom.MakeValid();

        //remove curves
        if (GeometryType=="CircularString" || GeometryType=="CompoundCurve" || GeometryType=="CurvePolygon")
        {
            geom = geom.STCurveToLine();
            GeometryType = geom.STGeometryType().Value;
        }

        if (GeometryType == "MultiLineString" || GeometryType == "LineString") return PathsToJson(geom);

        if (GeometryType == "Polygon" || GeometryType == "MultiPolygon") return RingsToJSON(geom);

        return "{\"err\": " + "Could Not Parse Geometry " + (String)geom.STAsText().ToSqlString() + "}";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static String PathsToJson(SqlGeometry Geometry)
    {
        System.Text.StringBuilder JSONBuilder;
        JSONBuilder= new System.Text.StringBuilder("{\"paths\":[");
        Int32 PathCount = (Int32)Geometry.STNumGeometries();
        for (Int32 PathNum = 1; PathNum <= PathCount; PathNum++)
        {
            if (PathNum > 1) JSONBuilder.Append(",");
            JSONBuilder.Append("[");
            Int32 PointCount = (Int32)Geometry.STGeometryN(PathNum).STNumPoints();
            for (Int32 PointNum = 1; PointNum <= PointCount; PointNum++)
            {
                if (PointNum > 1) JSONBuilder.Append(",");
                JSONBuilder.Append("[");
                JSONBuilder.Append(Geometry.STGeometryN(PathNum).STPointN(PointNum).STX.ToSqlString());
                JSONBuilder.Append(",");
                JSONBuilder.Append(Geometry.STGeometryN(PathNum).STPointN(PointNum).STY.ToSqlString());
                JSONBuilder.Append("]");
            }

            JSONBuilder.Append("]");
        }
        JSONBuilder.Append("],\"spatialReference\":{\"wkid\":");
        JSONBuilder.Append(Geometry.STSrid.ToSqlString().Value);
        JSONBuilder.Append("}}");
        return JSONBuilder.ToString();
    }

    private static String PointsToJSON(SqlGeometry Geometry)
    {       
        System.Text.StringBuilder JSONBuilder;
        JSONBuilder = new System.Text.StringBuilder("{\"points\":[");
        Int32 PointCount = (Int32)Geometry.STNumPoints();
        for (Int32 PointNum = 1; PointNum <= PointCount; PointNum++)
        {
            if (PointNum > 1) JSONBuilder.Append(",");
            JSONBuilder.Append("[");
            JSONBuilder.Append(Geometry.STPointN(PointNum).STX.ToSqlString().Value);
            JSONBuilder.Append(",");
            JSONBuilder.Append(Geometry.STPointN(PointNum).STY.ToSqlString().Value);
            JSONBuilder.Append("]");
        }
        JSONBuilder.Append("],\"spatialReference\":{\"wkid\":");
        JSONBuilder.Append(Geometry.STSrid.ToSqlString().Value);
        JSONBuilder.Append("}}");
        return JSONBuilder.ToString();
    }

    private static String RingsToJSON(SqlGeometry Geometry)
    {
        //Geometry = Geometry.STUnion(Geometry.STStartPoint());
        System.Text.StringBuilder JSONBuilder;
        JSONBuilder = new System.Text.StringBuilder("{");
        JSONBuilder.Append("\"rings\":[");

        Int32 PolyCount = (Int32)Geometry.STNumGeometries();
        for (Int32 PolyNum = 1; PolyNum <= PolyCount; PolyNum ++ )
        {
            if (PolyNum > 1) JSONBuilder.Append(",");
            //Add the exterior Ring
            SqlGeometry ExtRing;
            ExtRing = Geometry.STGeometryN(PolyNum).STExteriorRing();
            //if (ExtRing.IsNull || ExtRing.STIsEmpty().Equals(1)) return "{}";
            Int32 ERPointCount = (Int32)ExtRing.STNumPoints();
            JSONBuilder.Append("[");
            for (Int32 PointNum = ERPointCount; PointNum > 0; PointNum --)
            {
                if (PointNum < ERPointCount) JSONBuilder.Append(",");
                JSONBuilder.Append("[");
                JSONBuilder.Append(ExtRing.STPointN(PointNum).STX.ToString());
                JSONBuilder.Append(",");
                JSONBuilder.Append(ExtRing.STPointN(PointNum).STY.ToString());
                JSONBuilder.Append("]");   
            }
            JSONBuilder.Append("]");
            //Add the interior Rings
            Int32 RingCount = (Int32)Geometry.STGeometryN(PolyNum).STNumInteriorRing();
            
            for (Int32 RingNum = 1; RingNum <= RingCount; RingNum ++)
            {
                SqlGeometry IntRing = Geometry.STGeometryN(PolyNum).STInteriorRingN(RingNum);
                if (IntRing.IsNull || IntRing.STIsEmpty().Equals(1))
                {
                    JSONBuilder = new System.Text.StringBuilder("Poly: " + PolyNum.ToString() + " IntRing: " + RingNum.ToString());
                    return JSONBuilder.ToString();
                }
                else
                {
                    JSONBuilder.Append(",[");
                    Int32 IRPointCount = (Int32)IntRing.STNumPoints();
                    for (Int32 PointNum = 1; PointNum <= IRPointCount; PointNum++)
                    {
                        if (PointNum > 1) JSONBuilder.Append(",");
                        JSONBuilder.Append("[");
                        JSONBuilder.Append(IntRing.STPointN(PointNum).STX.ToString());
                        JSONBuilder.Append(",");
                        JSONBuilder.Append(IntRing.STPointN(PointNum).STY.ToString());
                        JSONBuilder.Append("]");
                    }
                    JSONBuilder.Append("]");
                }
            }

        }
        JSONBuilder.Append("],\"spatialReference\":{\"wkid\":");
        JSONBuilder.Append(Geometry.STSrid.ToSqlString().Value);
        JSONBuilder.Append("}}");
        return JSONBuilder.ToString();
    }

}
