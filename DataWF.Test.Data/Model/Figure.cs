using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Geometry;

namespace DataWF.Test.Data
{
    [Table(TestORM.FigureTableName, "Geometry")]
    public class Figure : DBItem
    {
        public static readonly DBTable<Figure> DBTable = GetTable<Figure>();
        public static readonly DBColumn PrimaryKey = DBTable.ParseProperty(nameof(Id));
        public static readonly DBColumn LocationKey = DBTable.ParseProperty(nameof(Location));
        public static readonly DBColumn BoxKey = DBTable.ParseProperty(nameof(Box));
        public static readonly DBColumn MatrixKey = DBTable.ParseProperty(nameof(Matrix));
        public static readonly DBColumn PolygonKey = DBTable.ParseProperty(nameof(Polygon));

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValueNullable<int>(PrimaryKey);
            set => SetValueNullable(value, PrimaryKey);
        }

        [Column("location", 16)]
        public Point2D Location
        {
            get => GetValue<Point2D>(LocationKey);
            set => SetValue(value, LocationKey);
        }

        [Column("box", 32)]
        public Rectangle2D Box
        {
            get => GetValue<Rectangle2D>(BoxKey);
            set => SetValue(value, BoxKey);
        }

        [Column("matrix", 72)]
        public Matrix2D Matrix
        {
            get => GetValue<Matrix2D>(MatrixKey);
            set => SetValue(value, MatrixKey);
        }

        [Column("polygon", 4000)]
        public Polygon2D Polygon
        {
            get => GetValue<Polygon2D>(PolygonKey);
            set => SetValue(value, PolygonKey);
        }
    }
}
