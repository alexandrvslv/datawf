using System;
using System.Globalization;

namespace DataWF.Invokers.Defaults;

[Invoker(ToGenerate = typeof(Nullable<>), Instance = true)]
public partial class NullableInvokers<T> where T : struct
{ }

[Invoker(ToGenerate = typeof(DateTime), Instance = true)]
public partial class DateTimeInvokers
{ }

[Invoker(ToGenerate = typeof(DateTimeOffset), Instance = true)]
public partial class DateTimeOffsetInvokers
{ }

[Invoker(ToGenerate = typeof(DateOnly), Instance = true)]
public partial class DateOnlyInvokers
{ }

[Invoker(ToGenerate = typeof(TimeOnly), Instance = true)]
public partial class TimeOnlyInvokers
{ }

[Invoker(ToGenerate = typeof(TimeSpan), Instance = true)]
public partial class TimeSpanInvokers
{ }

[Invoker(ToGenerate = typeof(Guid), Instance = true)]
public partial class GuidInvokers
{ }

[Invoker(ToGenerate = typeof(CultureInfo), Instance = true)]
public partial class CultureInfoInvokers
{ }

[Invoker(ToGenerate = typeof(Version), Instance = true)]
public partial class VersionInvokers
{ }

[Invoker(ToGenerate = typeof(Uri), Instance = true)]
public partial class UriInvokers
{ }