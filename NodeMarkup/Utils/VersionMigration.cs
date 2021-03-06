﻿using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utils
{
    public static class VersionMigration
    {
        public static Dictionary<ObjectId, ObjectId> Befor1_2(Markup markup, Dictionary<ObjectId, ObjectId> map)
        {
            if (map == null)
                map = new Dictionary<ObjectId, ObjectId>();

            foreach(var enter in markup.Enters)
            {
                foreach(var point in enter.Points.Skip(1).Take(enter.PointCount - 2))
                {
                    switch(point.PointType)
                    {
                        case MarkupPoint.Type.LeftEdge:
                            map[new ObjectId() { Point = point.Id }] = new ObjectId() { Point = point.Id - (1 << 16) };
                            break;
                        case MarkupPoint.Type.RightEdge:
                            map[new ObjectId() { Point = point.Id }] = new ObjectId() { Point = point.Id + (1 << 16) };
                            break;
                    }
                }
            }

            return map;
        }
    }
}
