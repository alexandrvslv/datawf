using DataWF.Common;
using DataWF.Data;
using DataWF.Geometry;

namespace DataWF.Test.Data
{
    [Table(TestORM.FigureTableName, "Geometry")]
    public partial class Figure : DBItem
    {
        public Figure(DBTable table) : base(table)
        { }

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(Table.IdKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("location", 16)]
        public Point2D? Location
        {
            get => GetProperty<Point2D?>();
            set => SetProperty(value);
        }

        [Column("box", 32)]
        public Rectangle2D? Box
        {
            get => GetProperty<Rectangle2D?>();
            set => SetProperty(value);
        }

        [Column("matrix", 72)]
        public Matrix2D Matrix
        {
            get => GetProperty<Matrix2D>();
            set => SetProperty(value);
        }

        [Column("polygon", 4000)]
        public Polygon2D Polygon
        {
            get => GetProperty<Polygon2D>();
            set => SetProperty(value);
        }
    }
}
