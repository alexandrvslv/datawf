using System;

namespace DataWF
{
    public interface INotifyValidation
    {
        event EventHandler<ValidationEventArgs> Validated;
        void OnValidated(ValidationEventArgs e);
    }
}