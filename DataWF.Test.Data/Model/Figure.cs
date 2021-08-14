using DataWF.Common;
using DataWF.Data;
using DataWF.Geometry;

namespace DataWF.Test.Data
{
    [Table(TestORM.FigureTableName, "Geometry")]
    public partial class Figure : DBItem
    {
        [Column("id", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("location", 16)]
        public Point2D? Location
        {
            get => GetValue<Point2D?>();
            set => SetValue(value);
        }

        [Column("box", 32)]
        public Rectangle2D? Box
        {
            get => GetValue<Rectangle2D?>();
            set => SetValue(value);
        }

        [Column("matrix", 72)]
        public Matrix2D Matrix
        {
            get => GetValue<Matrix2D>();
            set => SetValue(value);
        }

        [Column("polygon", 4000)]
        public Polygon2D Polygon
        {
            get => GetValue<Polygon2D>();
            set => SetValue(value);
        }
    }
}
