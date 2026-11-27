using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;
using System.Text;

namespace DataWF.Generator
{
    internal abstract class BaseGenerator : IEquatable<BaseGenerator>
    {
        public static bool Validate(ISymbol symbol)
        {
            return symbol is INamedTypeSymbol;
        }

        protected StringBuilder source = new();
        protected readonly TypeRecordProvider provider;
        private readonly TypeRecord typeRecord;

        public BaseGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol, TakeBaseMode takeBase, Func<IPropertySymbol, bool>? validateProperty = null)
            : this(provider, provider.GetOrCreate(
                forceRecreate: true,
                typeSymbol: typeSymbol,
                takeBase: takeBase,
                validateProperty: validateProperty,
                validateAttribute: TypeRecord.AllAttributes)!.Type!)
        { }

        public BaseGenerator(TypeRecordProvider provider, TypeRecord typeRecord)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.typeRecord = typeRecord ?? throw new ArgumentNullException(nameof(typeRecord));
        }

        public StringBuilder Source
        {
            get => source;
            set => source = value;
        }

        public string GenericArgs => typeRecord.GenericArgs;

        public string TypeName => typeRecord.OriginName;

        public string Namespace => typeRecord.Namespace;

        public TypeRecord Type => typeRecord;

        public virtual string GetFileName() => $"{TypeName}{GetType().Name}.cs";

        public bool Process(ref SourceProductionContext context)
        {
            try
            {

                var result = ProcessInternal(ref context);
                if (result)
                {
                    try { context.ReportDiagnostic(Diagnostic.Create(Helper.DDSuccessGeneration, Location.None, GetType().Name, TypeName)); } catch { }
                }
                return result;
            }
            catch (Exception ex)
            {
                try { context.ReportDiagnostic(Diagnostic.Create(Helper.DDFailGeneration, Location.None, GetType().Name, TypeName, ex.Message, ex.StackTrace)); } catch { }
                return false;
            }
        }

        public abstract bool ProcessInternal(ref SourceProductionContext context);

        public override bool Equals(object? obj) => obj is BaseGenerator generator && Equals(generator);

        public virtual bool Equals(BaseGenerator? other) => typeRecord.Equals(other?.Type);

        public override int GetHashCode() => typeRecord.GetHashCode();
    }

}