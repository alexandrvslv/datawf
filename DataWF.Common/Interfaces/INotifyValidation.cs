using System;

namespace DataWF.Common
{
    public interface INotifyValidation
    {
        event EventHandler<ValidationEventArgs> Validated;
        void OnValidated(ValidationEventArgs e);
    }
}